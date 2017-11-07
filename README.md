# REST Client for Unity
For Unity developers looking to use REST Services in their Unity game / app.

**RESTClient** for Unity is built on top of [UnityWebRequest](https://docs.unity3d.com/Manual/UnityWebRequest.html) and Unity's [JsonUtility](https://docs.unity3d.com/ScriptReference/JsonUtility.html) to make it easier to compose REST requests and return the results serialized as native C# data model objects.

## Features
 - Methods to add request body, headers and query strings to REST requests.
 - Ability to return result as native object or as plain text.
 - Work around for nested arrays.
 - Work around for parsing abstract types on UWP.

## How do I use this with cloud services?
Checkout the following projects for Unity which were built using this REST Client library as  examples.
 - [Azure App Services](https://github.com/Unity3dAzure/AppServices)
 - [Azure Blob Storage](https://github.com/Unity3dAzure/StorageServices)
 - [Azure Functions](https://github.com/Unity3dAzure/AzureFunctions)
 - [Nether (serverless)](https://github.com/MicrosoftDX/nether/tree/serverless/src/Client/Unity)

## Requirements
Requires Unity v5.3 or greater as [UnityWebRequest](https://docs.unity3d.com/Manual/UnityWebRequest.html) and [JsonUtility](https://docs.unity3d.com/ScriptReference/JsonUtility.html) features are used. Unity will be extending platform support for UnityWebRequest so keep Unity up to date if you need to support these additional platforms.

## Example Usage
This snippet shows how to send a **POST** request to a new [Azure Function](https://azure.microsoft.com/en-gb/services/functions/) HTTP Trigger "hello" sample function:

```
using RESTClient;
using System;
```

```
public class RESTClientExample : MonoBehaviour {

	void Start () {
		StartCoroutine( SayHello(SayHelloCompleted) );
	}

	private IEnumerator SayHello(Action<IRestResponse<string>> callback = null) {
		RestRequest request = new RestRequest("https://***.azurewebsites.net/api/hello", Method.POST);
		request.AddHeader("Content-Type", "application/json");
		request.AddQueryParam("code", "***");
		request.AddBody("{\"name\": \"unity\"}");
		yield return request.Request.Send();
		request.GetText(callback);
	}

	private void SayHelloCompleted(IRestResponse<string> response) {
		if (response.IsError) {
			Debug.LogError("Request error: " + response.StatusCode);
			return;
		}
		Debug.Log("Completed: " + response.Content);
	}

}
```

## Supported platforms
Intended to work on all the platforms [UnityWebRequest](https://docs.unity3d.com/Manual/UnityWebRequest.html) supports including:
* Unity Editor (Mac/PC) and Standalone players
* iOS
* Android
* Windows 10 (UWP)

## Troubleshooting
- Remember to wrap async calls with [`StartCoroutine()`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.StartCoroutine.html)
- Before building for a target platform remember to check the **Internet Client capability** is enabled in the Unity player settings, otherwise all REST calls will fail with a '-1' status error.

Questions or tweet [@deadlyfingers](https://twitter.com/deadlyfingers)
