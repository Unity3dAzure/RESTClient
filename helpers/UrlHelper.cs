// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Collections.Generic;

namespace RESTClient {
  public static class UrlHelper {

    /// <summary>
		/// The returned url format will be: baseUrl + path(s) + query string
		/// </summary>
		/// <returns>The URL.</returns>
		/// <param name="baseUrl">Base URL.</param>
		/// <param name="queryParams">Query parameters.</param>
		/// <param name="paths">Paths.</param>
    public static string BuildQuery(string baseUrl, Dictionary<string, string> queryParams = null, params string[] paths) {
      StringBuilder q = new StringBuilder();
      if (queryParams == null) {
        return BuildQuery(baseUrl, "", paths);
      }

      foreach (KeyValuePair<string, string> param in queryParams) {
        if (q.Length == 0) {
          q.Append("?");
        } else {
          q.Append("&");
        }
        q.Append(param.Key + "=" + param.Value);
      }

      return BuildQuery(baseUrl, q.ToString(), paths);
    }

    public static string BuildQuery(string baseUrl, string queryString, params string[] paths) {
      StringBuilder sb = new StringBuilder();
      sb.Append(baseUrl);
      if (!baseUrl.EndsWith("/")) {
        sb.Append("/");
      }

      foreach (string path in paths) {
        if (!path.EndsWith("/")) {
          sb.Append(path);
        } else {
          sb.Append(path + "/");
        }
      }

      sb.Append(queryString);
      return sb.ToString();
    }
  }
}

