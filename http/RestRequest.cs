// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace RESTClient {
  public class RestRequest : IDisposable {
    public UnityWebRequest Request { get; private set; }

    private QueryParams queryParams;

    public RestRequest(UnityWebRequest request) {
      this.Request = request;
    }

    public RestRequest(string url, Method method) {
      Request = new UnityWebRequest(url, method.ToString());
      Request.downloadHandler = new DownloadHandlerBuffer();
    }

    public void AddHeader(string key, string value) {
      Request.SetRequestHeader(key, value);
    }

    public void AddHeaders(Dictionary<string, string> headers) {
      foreach (KeyValuePair<string, string> header in headers) {
        AddHeader(header.Key, header.Value);
      }
    }

    public void AddBody(string text, string contentType = "text/plain; charset=UTF-8") {
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      this.AddBody(bytes, contentType);
    }

    public void AddBody(byte[] bytes, string contentType) {
      if (Request.uploadHandler != null) {
        Debug.LogWarning("Request body can only be set once");
        return;
      }
      Request.uploadHandler = new UploadHandlerRaw(bytes);
      Request.uploadHandler.contentType = contentType;
    }

    public virtual void AddBody<T>(T data, string contentType = "application/json; charset=utf-8") {
      if (typeof(T) == typeof(string)) {
        this.AddBody(data.ToString(), contentType);
        return;
      }
      string jsonString = JsonUtility.ToJson(data);
      byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
      this.AddBody(bytes, contentType);
    }

    public virtual void AddQueryParam(string key, string value, bool shouldUpdateRequestUrl = false) {
      if (queryParams == null) {
        queryParams = new QueryParams();
      }
      queryParams.AddParam(key, value);
      if (shouldUpdateRequestUrl) {
        UpdateRequestUrl();
      }
    }

    public void SetQueryParams(QueryParams queryParams) {
      if (this.queryParams != null) {
        Debug.LogWarning("Replacing previous query params");
      }
      this.queryParams = queryParams;
    }

    public virtual void UpdateRequestUrl() {
      if (queryParams == null) {
        return;
      }
      var match = Regex.Match(Request.url, @"^(.+)(\\?)(.+)", RegexOptions.IgnoreCase);
      if (match.Groups.Count == 4 && match.Groups[0].Value.Length > 0) {
        string url = match.Groups[0].Value + queryParams.ToString();
        Request.url = url;
      }
    }

    #region Response and object parsing

    private RestResult GetRestResult(bool expectedBodyContent = true) {
      HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), Request.responseCode.ToString());
      RestResult result = new RestResult(statusCode);

      if (result.IsError) {
        result.ErrorMessage = "Response failed with status: " + statusCode.ToString();
        return result;
      }

      if (expectedBodyContent && string.IsNullOrEmpty(Request.downloadHandler.text)) {
        result.IsError = true;
        result.ErrorMessage = "Response has empty body";
        return result;
      }
      return result;
    }

    private RestResult<T> GetRestResult<T>() {
      HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), Request.responseCode.ToString());
      RestResult<T> result = new RestResult<T>(statusCode);

      if (result.IsError) {
        result.ErrorMessage = "Response failed with status: " + statusCode.ToString();
        return result;
      }

      if (string.IsNullOrEmpty(Request.downloadHandler.text)) {
        result.IsError = true;
        result.ErrorMessage = "Response has empty body";
        return result;
      }

      return result;
    }

    #endregion

    #region JSON object parsing response

    /// <summary>
    /// Shared method to return response result whether an object or array of objects
    /// </summary>
    private RestResult<T> TryParseJsonArray<T>() {
      RestResult<T> result = GetRestResult<T>();
      // try parse an array of objects
      try {
        result.AnArrayOfObjects = JsonHelper.FromJsonArray<T>(Request.downloadHandler.text);
      } catch (Exception e) {
        result.IsError = true;
        result.ErrorMessage = "Failed to parse an array of objects of type: " + typeof(T).ToString() + " Exception message: " + e.Message;
      }
      return result;
    }

    private RestResult<T> TryParseJson<T>() {
      RestResult<T> result = GetRestResult<T>();
      // try parse an object
      try {
        result.AnObject = JsonUtility.FromJson<T>(Request.downloadHandler.text);
      } catch (Exception e) {
        result.IsError = true;
        result.ErrorMessage = "Failed to parse object of type: " + typeof(T).ToString() + " Exception message: " + e.Message;
      }
      return result;
    }

    /// <summary>
    /// Parses object with T data = JsonUtil.FromJson<T>, then callback RestResponse<T>
    /// </summary>
    public IRestResponse<T> ParseJson<T>(Action<IRestResponse<T>> callback = null) {
      RestResult<T> result = TryParseJson<T>();
      RestResponse<T> response;
      if (result.IsError) {
        Debug.LogWarning("Response error status:" + result.StatusCode + " code:" + Request.responseCode + " error:" + result.ErrorMessage + " Request url:" + Request.url);
        response = new RestResponse<T>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse<T>(result.StatusCode, Request.url, Request.downloadHandler.text, result.AnObject);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    /// <summary>
    /// Parses array of objects with T[] data = JsonHelper.GetJsonArray<T>, then callback RestResponse<T[]>
    /// </summary>
    public IRestResponse<T[]> ParseJsonArray<T>(Action<IRestResponse<T[]>> callback = null) {
      RestResult<T> result = TryParseJsonArray<T>();
      RestResponse<T[]> response;
      if (result.IsError) {
        Debug.LogWarning("Response error status:" + result.StatusCode + " code:" + Request.responseCode + " error:" + result.ErrorMessage + " Request url:" + Request.url);
        response = new RestResponse<T[]>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse<T[]>(result.StatusCode, Request.url, Request.downloadHandler.text, result.AnArrayOfObjects);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    private RestResult<N> TryParseJsonNestedArray<T, N>(string namedArray) where N : INestedResults<T>, new() {
      RestResult<N> result = GetRestResult<N>();
      // try parse an object
      try {
        result.AnObject = JsonHelper.FromJsonNestedArray<T, N>(Request.downloadHandler.text, namedArray); //JsonUtility.FromJson<N>(request.downloadHandler.text);
      } catch (Exception e) {
        result.IsError = true;
        result.ErrorMessage = "Failed to parse object of type: " + typeof(N).ToString() + " Exception message: " + e.Message;
      }
      return result;
    }

    /// Work-around for nested array
    public RestResponse<N> ParseJsonNestedArray<T, N>(string namedArray, Action<IRestResponse<N>> callback = null) where N : INestedResults<T>, new() {
      RestResult<N> result = TryParseJsonNestedArray<T, N>(namedArray);
      RestResponse<N> response;
      if (result.IsError) {
        Debug.LogWarning("Response error status:" + result.StatusCode + " code:" + Request.responseCode + " error:" + result.ErrorMessage + " request url:" + Request.url);
        response = new RestResponse<N>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse<N>(result.StatusCode, Request.url, Request.downloadHandler.text, result.AnObject);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    #endregion

    #region XML object parsing response

    private RestResult<T> TrySerializeXml<T>() {
      RestResult<T> result = GetRestResult<T>();
      // return early if there was a status / data error other than Forbidden
      if (result.IsError && result.StatusCode == HttpStatusCode.Forbidden) {
        Debug.LogWarning("Authentication Failed: " + Request.downloadHandler.text);
        return result;
      } else if (result.IsError) {
        return result;
      }
      // otherwise try and serialize XML response text to an object
      try {
        result.AnObject = XmlHelper.FromXml<T>(Request.downloadHandler.text);
      } catch (Exception e) {
        result.IsError = true;
        result.ErrorMessage = "Failed to parse object of type: " + typeof(T).ToString() + " Exception message: " + e.Message;
      }
      return result;
    }

    public RestResponse<T> ParseXML<T>(Action<IRestResponse<T>> callback = null) {
      RestResult<T> result = TrySerializeXml<T>();
      RestResponse<T> response;
      if (result.IsError) {
        Debug.LogWarning("Response error status:" + result.StatusCode + " code:" + Request.responseCode + " error:" + result.ErrorMessage + " request url:" + Request.url);
        response = new RestResponse<T>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse<T>(result.StatusCode, Request.url, Request.downloadHandler.text, result.AnObject);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    /// <summary>
    /// To be used with a callback which passes the response with result including status success or error code, request url and any body text.
    /// </summary>
    /// <param name="callback">Callback.</param>
    public RestResponse Result(Action<RestResponse> callback = null) {
      return GetText(callback, false);
    }

    public RestResponse GetText(Action<RestResponse> callback = null, bool expectedBodyContent = true) {
      RestResult result = GetRestResult(expectedBodyContent);
      RestResponse response;
      if (result.IsError) {
        Debug.LogWarning("Response error status:" + result.StatusCode + " code:" + Request.responseCode + " error:" + result.ErrorMessage + " request url:" + Request.url);
        response = new RestResponse(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse(result.StatusCode, Request.url, Request.downloadHandler.text);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    #endregion

    /// <summary>
    /// Return response body as plain text.
    /// </summary>
    /// <param name="callback">Callback with response type: IRestResponse<string></param>
    public IRestResponse<T> GetText<T>(Action<IRestResponse<T>> callback = null) {
      RestResult<string> result = GetRestResult<string>();
      RestResponse<T> response;
      if (result.IsError) {
        response = new RestResponse<T>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse<T>(result.StatusCode, Request.url, Request.downloadHandler.text);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    /// <summary>
    /// Return response body as bytes.
    /// </summary>
    /// <param name="callback">Callback with response type: IRestResponse<byte[]></param>
    public IRestResponse<byte[]> GetBytes(Action<IRestResponse<byte[]>> callback = null) {
      RestResult result = GetRestResult(false);
      RestResponse<byte[]> response;
      if (result.IsError) {
        response = new RestResponse<byte[]>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        response = new RestResponse<byte[]>(result.StatusCode, Request.url, null, Request.downloadHandler.data);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    #region Handle native asset UnityWebRequest.Get... requests

    public IRestResponse<Texture> GetTexture(Action<IRestResponse<Texture>> callback = null) {
      RestResult result = GetRestResult(false);
      RestResponse<Texture> response;
      if (result.IsError) {
        response = new RestResponse<Texture>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        Texture texture = ((DownloadHandlerTexture)Request.downloadHandler).texture;
        response = new RestResponse<Texture>(result.StatusCode, Request.url, null, texture);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    public IRestResponse<AudioClip> GetAudioClip(Action<IRestResponse<AudioClip>> callback = null) {
      RestResult result = GetRestResult(false);
      RestResponse<AudioClip> response;
      if (result.IsError) {
        response = new RestResponse<AudioClip>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        AudioClip audioClip = ((DownloadHandlerAudioClip)Request.downloadHandler).audioClip;
        response = new RestResponse<AudioClip>(result.StatusCode, Request.url, null, audioClip);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    public IRestResponse<AssetBundle> GetAssetBundle(Action<IRestResponse<AssetBundle>> callback = null) {
      RestResult result = GetRestResult(false);
      RestResponse<AssetBundle> response;
      if (result.IsError) {
        response = new RestResponse<AssetBundle>(result.ErrorMessage, result.StatusCode, Request.url, Request.downloadHandler.text);
      } else {
        AssetBundle assetBundle = ((DownloadHandlerAssetBundle)Request.downloadHandler).assetBundle;
        response = new RestResponse<AssetBundle>(result.StatusCode, Request.url, null, assetBundle);
      }
      if (callback != null) {
        callback(response);
      }
      this.Dispose();
      return response;
    }

    #endregion

    public UnityWebRequestAsyncOperation Send() {
      return Request.SendWebRequest(); // Send() is obsolete UNITY_2017_2_OR_NEWER
    }

    public void Dispose() {
      Request.Dispose(); // Request completed, clean-up resources
    }

  }
}
