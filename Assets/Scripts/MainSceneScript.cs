using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class MainSceneScript : MonoBehaviour
{
	public Text endpointHeading;
	public GameObject listContent, detailsContent;
	public GameObject btnPrefab, itemPrefab, toast;
	public Button[] endpointBtns;

	public string baseUrl = "http://127.0.0.1:5000";

	List<string> endpoints;

	string currentEndPoint = "";
	string currentAddSchema = "";
	string parseEndpoint = "";
	string currentObjectId = "";

	JSONArray backupData;
	Dictionary<string, string> objectIdDict;

	void Start()
	{
		objectIdDict = new Dictionary<string, string>();
		endpoints = new List<string>();
		endpoints.Add("/fablab/api/users/");
		currentEndPoint = endpoints[0];
		parseEndpoint = "FablabUser";
		endpointHeading.text = parseEndpoint;

		endpointBtns[0].onClick.AddListener(delegate {OnEndpointClick(endpoints[0], "FablabUser"); });

		StartCoroutine(LoadEndpoint(currentEndPoint));
	}

	public void OnEndpointClick(string url, string parseEndpointnew)
	{
		CleanListContent();
		CleanDetailsContent();
		Debug.Log(parseEndpointnew);
		parseEndpoint = parseEndpointnew;
		endpointHeading.text = parseEndpoint;
		currentEndPoint = url;
		StartCoroutine(LoadEndpoint(currentEndPoint));
	}

	void CleanListContent()
	{
		foreach(var obj in listContent.GetComponentsInChildren<Button>())
		{
			Destroy(obj.gameObject);
		}
	}

	IEnumerator LoadEndpoint(string endpointUrl)
	{
		ResetEndpointUrls();
		string url = baseUrl + endpointUrl;

		currentObjectId = "";
		objectIdDict.Clear();

		yield return StartCoroutine(BackupGet(null));

		using (WWW www = new WWW(url))
		{
			yield return www;

			if(www.error == null)
			{
				var data = JSON.Parse(www.text);

				string schemaUrl = baseUrl + data["@controls"]["schema"]["schemaUrl"];
				string addSchemaUrl = baseUrl + data["@controls"]["add"]["schemaUrl"];

				using (WWW www1 = new WWW(schemaUrl))
				{
					yield return www1;
					var schema = JSON.Parse(www1.text);

					LoadButtons(data, schema, endpointUrl);
				}

				using (WWW www2 = new WWW(addSchemaUrl))
				{
					yield return www2;
					currentAddSchema = www2.text;
				}

				var endpointUrls = data["@controls"]["endpoints"].AsArray;
				LoadEndpointUrls(endpointUrls);
			}
		}
	}

	void ResetEndpointUrls()
	{
		for(int i=1; i<4; i++)
		{
			endpointBtns[i].gameObject.SetActive(false);
		}
	}

	void LoadEndpointUrls(JSONArray endpointUrls)
	{
		endpoints.Clear();
		endpoints.Add("/fablab/api/users/");
		int i=1;
		foreach(var obj in endpointUrls)
		{
			string title = obj.Value["title"];
			string url = obj.Value["endpointUrl"];

			endpoints.Add(url);
			endpointBtns[i].gameObject.SetActive(true);
			endpointBtns[i].name = title;
			endpointBtns[i].GetComponentInChildren<Text>().text = title;
			endpointBtns[i].onClick.RemoveAllListeners();
			endpointBtns[i].onClick.AddListener(delegate {OnEndpointClick(url, title); });
			i++;
		}
	}

	void LoadButtons(JSONNode data, JSONNode schema, string endpointUrl)
	{
		List<string> properties = new List<string>();

		foreach(var obj in schema["properties"].AsArray)
		{
			properties.Add(obj.Value["title"]);
		}

		foreach(var obj in data["items"].AsArray)
		{
			GenerateButton(obj.Value[0], endpointUrl);
			string tempId = "";
			if(parseEndpoint == "FablabUser")
			{
				tempId = obj.Value["username"];
			}
			else if(parseEndpoint == "Machines")
			{
				tempId = obj.Value["machineID"];
			}
			else if(parseEndpoint == "MachineTypes")
			{
				tempId = obj.Value["id"];
			}
			else if(parseEndpoint == "Reservations")
			{
				tempId = obj.Value["reservationID"];
			}

			foreach(var obj1 in backupData)
			{
				if(obj1.Value["originalId"].Equals(tempId))
				{
					Debug.Log(tempId);
					objectIdDict.Add(tempId, obj1.Value["objectId"]);
				}
			}
		}
	}

	void GenerateButton(string id, string endpointUrl)
	{
		var obj = Instantiate(btnPrefab, listContent.transform);
		obj.name = id;
		obj.GetComponentInChildren<Text>().text = id;
		var link = baseUrl + endpointUrl + id + "/";
		obj.GetComponent<Button>().onClick.AddListener(delegate {OnBtnClick(link, id); });
	}

	void OnBtnClick(string link, string id)
	{
		CleanDetailsContent();
		StartCoroutine(LoadItemData(link, id));
		currentObjectId = objectIdDict[id];
	}

	void CleanDetailsContent()
	{
		foreach(var obj in detailsContent.GetComponentsInChildren<Item>())
		{
			Destroy(obj.gameObject);
		}
		foreach(var obj in detailsContent.GetComponentsInChildren<Button>())
		{
			Destroy(obj.gameObject);
		}
	}

	IEnumerator LoadItemData(string url, string id)
	{
		using (WWW www = new WWW(url))
		{
			yield return www;
			if(www.error == null)
			{
				var data = JSON.Parse(www.text);

				string schemaUrl = baseUrl + data["@controls"]["schema"]["schemaUrl"];
				using (WWW www1 = new WWW(schemaUrl))
				{
					yield return www1;
					var schema = JSON.Parse(www1.text);

					LoadDetails(data, schema, id, url);
				}
			}
		}
	}

	void LoadDetails(JSONNode data, JSONNode schema, string id, string url)
	{
		List<string> properties = new List<string>();

		foreach(var obj in schema["properties"].AsArray)
		{
			properties.Add(obj.Value["title"]);
		}

		foreach(var prop in properties)
		{
			GenerateDetails(prop, data[prop]);
		}

		var editBtn = Instantiate(btnPrefab, detailsContent.transform);
		editBtn.GetComponentInChildren<Text>().text = "Edit";
		editBtn.GetComponent<Button>().onClick.AddListener(delegate {OnEditClick(editBtn, url, id); });

		if(parseEndpoint != "Reservations")
		{
			var deleteBtn = Instantiate(btnPrefab, detailsContent.transform);
			deleteBtn.GetComponentInChildren<Text>().text = "Delete";
			deleteBtn.GetComponent<Button>().onClick.AddListener(delegate {OnDeleteClick(url); });
		}
	}

	void GenerateDetails(string key, string value, bool required = false)
	{
		var obj = Instantiate(itemPrefab, detailsContent.transform);
		obj.transform.GetChild(1).GetComponent<Text>().text = key;
		obj.GetComponentInChildren<InputField>().text = value;
		if(required)
		{
			obj.transform.GetChild(1).GetComponent<Text>().fontStyle = FontStyle.Bold;
		}
	}

	public void OnAddClick()
	{
		CleanDetailsContent();
		Dictionary<string, bool> properties = new Dictionary<string, bool>();

		var schema = JSON.Parse(currentAddSchema);

		foreach(var obj in schema["properties"].AsArray)
		{
			properties.Add(obj.Value["title"], false);
		}
		foreach(var obj in schema["required"].AsArray)
		{
			properties[obj.Value] = true;
		}
		foreach(var prop in properties)
		{
			GenerateDetails(prop.Key, "", prop.Value);
		}

		var items = detailsContent.GetComponentsInChildren<Item>();
		foreach(var obj in items)
		{
			obj.transform.GetComponentInChildren<ItemInputField>().gameObject.GetComponent<InputField>().interactable = true;
		}

		var saveBtn = Instantiate(btnPrefab, detailsContent.transform);
		saveBtn.GetComponentInChildren<Text>().text = "Save";
		saveBtn.GetComponent<Button>().onClick.AddListener(OnAddSubmitClick);
	}

	void OnAddSubmitClick()
	{
		var dataDict = new Dictionary<string, string>();
		foreach(var obj in detailsContent.GetComponentsInChildren<Item>())
		{
			dataDict.Add(obj.transform.GetChild(1).GetComponent<Text>().text, obj.transform.GetChild(0).GetComponent<InputField>().text);
		}

		StartCoroutine(Post(dataDict));
	}

	void OnEditClick(GameObject btn, string url, string id)
	{
		var items = detailsContent.GetComponentsInChildren<Item>();
		foreach(var obj in items)
		{
			string text = obj.GetComponentInChildren<ItemText>().gameObject.GetComponent<Text>().text;
			if(text != "createdAt" && text != "updatedAt")
			{
				obj.transform.GetComponentInChildren<ItemInputField>().gameObject.GetComponent<InputField>().interactable = true;
			}
		}

		btn.GetComponentInChildren<Text>().text = "Save";
		btn.GetComponent<Button>().onClick.RemoveAllListeners();
		btn.GetComponent<Button>().onClick.AddListener(delegate {OnSaveClick(btn, url, id); });
	}

	void OnSaveClick(GameObject btn, string url, string id)
	{
		var dataDict = new Dictionary<string, string>();
		foreach(var obj in detailsContent.GetComponentsInChildren<Item>())
		{
			dataDict.Add(obj.transform.GetChild(1).GetComponent<Text>().text, obj.transform.GetChild(0).GetComponent<InputField>().text);
		}

		StartCoroutine(Put(btn, url, id, dataDict));
	}

	void OnDeleteClick(string url)
	{
		StartCoroutine(Delete(url));
	}

	IEnumerator Post(Dictionary<string, string> data)
	{
		string url = baseUrl + currentEndPoint;
		var jsonData = ToJson(data);
		Debug.Log(url);
		Debug.Log(jsonData);
		var request = new UnityWebRequest(url, "POST");
		byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
		request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
		request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		
		yield return request.Send();

		if (request.isNetworkError || request.isHttpError)
		{
			Debug.Log(request.error);
			ShowTaost("Add Failed");
		}
		else
		{
			Debug.Log("Upload complete!");
			ShowTaost("Add Success");

			var inData = JSON.Parse(request.downloadHandler.text);
			data.Add("originalId", inData["id"]);
			CleanListContent();
			CleanDetailsContent();
			yield return StartCoroutine(BackupPost(ToJson(data)));
			StartCoroutine(LoadEndpoint(currentEndPoint));
		}
	}

	IEnumerator Put(GameObject btn, string url, string id, Dictionary<string,string> data)
	{
		UnityWebRequest request = UnityWebRequest.Put(url, ToJson(data));
		request.SetRequestHeader("Content-Type", "application/json");

		yield return request.Send();

		if (request.isNetworkError || request.isHttpError)
		{
			Debug.Log(request.error);
			ShowTaost("Save Failed");
		}
		else
		{
			Debug.Log("Upload complete!");
			ShowTaost("Save Success");
			ResetSave(btn, url, id);
			StartCoroutine(BackupPut(data, currentObjectId));
		}
	}

	IEnumerator Delete(string url)
	{
		UnityWebRequest request = UnityWebRequest.Delete(url);

		yield return request.Send();

		if (request.isNetworkError || request.isHttpError)
		{
			Debug.Log(request.error);
			ShowTaost("Delete Failed");
		}
		else
		{
			Debug.Log("Delete complete!");
			ShowTaost("Delete Success");
			StartCoroutine(BackupDelete(currentObjectId));

			CleanListContent();
			CleanDetailsContent();
			StartCoroutine(LoadEndpoint(currentEndPoint));
		}
	}

	void ResetSave(GameObject btn, string url, string id)
	{
		var items = detailsContent.GetComponentsInChildren<Item>();
		foreach(var obj in items)
		{
			obj.transform.GetComponentInChildren<ItemInputField>().gameObject.GetComponent<InputField>().interactable = false;
		}

		btn.GetComponentInChildren<Text>().text = "Edit";
		btn.GetComponent<Button>().onClick.RemoveAllListeners();
		btn.GetComponent<Button>().onClick.AddListener(delegate {OnEditClick(btn, url, id); });
	}

	string ToJson(Dictionary<string, string> dictionary)
	{
		string data = "{";
		string tempStr;

		foreach(var obj in dictionary)
		{
			tempStr = "\"" + obj.Key + "\":\"" + obj.Value + "\",";
			data += tempStr;
		}
		data = data.Remove(data.Length-1);
		data += "}";
		return data;
	}

	void ShowTaost(string text)
	{
		toast.SetActive(true);
		toast.GetComponent<Text>().text = text;
		Invoke("HideToast", 2f);
	}

	void HideToast()
	{
		toast.SetActive(false);
	}

	IEnumerator BackupPost(string json)
	{
		byte[] postData = System.Text.Encoding.ASCII.GetBytes(json);

		string url = "https://parseapi.back4app.com/classes/" + parseEndpoint;
		Debug.Log(url);

		Dictionary<string, string> headers;

		using (WWW www = new WWW(url, postData, ParseHeaders()))
		{
			yield return www;
			Debug.Log(www.text);
			if(www.error != null)
			{
				Debug.Log(www.error);
			}
			else
			{
				Debug.Log("Object Saved");
			}
		}
	}

	IEnumerator BackupGet(string objectId)
	{
		string url = "";

		if(objectId == null)
			url = "https://parseapi.back4app.com/classes/" + parseEndpoint;
		else
			url = "https://parseapi.back4app.com/classes/" + parseEndpoint + "/" + objectId;

		Debug.Log(url);

		using (WWW www = new WWW(url, null, ParseHeaders()))
		{
			yield return www;
			Debug.Log(www.text);
			if(www.error != null)
			{
				Debug.Log(www.error);
			}
			else
			{
				var data = JSON.Parse(www.text);
				if(objectId == null)
				{
					Debug.Log("Objects Retrieved");
					backupData = data["results"].AsArray;
				}
				else
				{
					Debug.Log("Object Retrieved");
					currentObjectId = data["objectId"];
				}

			}
		}
	}

	IEnumerator BackupPut(Dictionary<string,string> dataDict, string objectId)
	{
		dataDict.Remove("createdAt");
		dataDict.Remove("updatedAt");
		string json = ToJson(dataDict);

		byte[] postData = System.Text.Encoding.ASCII.GetBytes(json);

		string url = "https://parseapi.back4app.com/classes/" + parseEndpoint + "/" + objectId;
		Debug.Log(url);

		var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);

		request.SetRequestHeader("Content-Type", "application/json");
		request.SetRequestHeader("X-Parse-Application-Id", "UaOzlF8QOmNZOuNlK2JuD6yrAoOLd6wc8YgwYdfP");
		request.SetRequestHeader("X-Parse-REST-API-Key", "lO0jaJcYq5l3MySQU6EcKDzi875pzhllPzR0YtgB");
		request.SetRequestHeader("X-Parse-Session-Token", PlayerPrefs.GetString(PlayerPrefs.GetString("objectId")));

		UploadHandlerRaw uploadHandler = new UploadHandlerRaw(postData);
		request.uploadHandler = uploadHandler;
		request.downloadHandler = new DownloadHandlerBuffer();

		yield return request.SendWebRequest();

		if (request.isError) // Error
		{
			Debug.Log(request.error);
		}
		else // Success
		{
			Debug.Log(request.downloadHandler.text);
			Debug.Log("Object Updated");
		}
	}

	IEnumerator BackupDelete(string objectId)
	{
		string url = "https://parseapi.back4app.com/classes/" + parseEndpoint + "/" + objectId;
		Debug.Log(url);

		var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE);
		request.SetRequestHeader("Content-Type", "application/json");
		request.SetRequestHeader("X-Parse-Application-Id", "UaOzlF8QOmNZOuNlK2JuD6yrAoOLd6wc8YgwYdfP");
		request.SetRequestHeader("X-Parse-REST-API-Key", "lO0jaJcYq5l3MySQU6EcKDzi875pzhllPzR0YtgB");
		request.SetRequestHeader("X-Parse-Session-Token", PlayerPrefs.GetString(PlayerPrefs.GetString("objectId")));
		request.downloadHandler = new DownloadHandlerBuffer();

		yield return request.SendWebRequest();

		if (request.isError) // Error
		{
			Debug.Log(request.error);
		}
		else // Success
		{
			Debug.Log(request.downloadHandler.text);
			Debug.Log("Object Deleted");
		}
	}

	Dictionary<string,string> ParseHeaders()
	{
		WWWForm form = new WWWForm();
		Dictionary<string, string> headers = form.headers;

		if (headers.ContainsKey("Content-Type"))
			headers["Content-Type"] = "application/json";
		else
			headers.Add("Content-Type", "application/json");

		headers.Add("X-Parse-Application-Id", "UaOzlF8QOmNZOuNlK2JuD6yrAoOLd6wc8YgwYdfP");
		headers.Add("X-Parse-REST-API-Key", "lO0jaJcYq5l3MySQU6EcKDzi875pzhllPzR0YtgB");
		headers.Add("X-Parse-Session-Token", PlayerPrefs.GetString(PlayerPrefs.GetString("objectId")));
		return headers;
	}
}
