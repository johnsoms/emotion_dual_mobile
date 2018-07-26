using UnityEngine;
using System.Collections;

using System;
using System.IO;
using UnityEngine.UI;

using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Collections.Generic;

#if NETFX_CORE
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;
#endif

public class Receiver2 : MonoBehaviour {

//	public UIManager uiManagerScript;

	public bool enableLog = false;
	public bool enableLogData = false;
	private string sourceURL;
	[HideInInspector]
	public Texture2D receivedTexture;

	// Use this for initialization
	void Start()
	{
		sourceURL = PlayerPrefs.GetString ("sourceURL");
		receivedTexture = new Texture2D(0, 0);
		GetVideo();
//		StartCoroutine(ReceiveData());
	}

	private static string GetAndroidFriendlyFilesPath()
	{
//		#if UNITY_ANDROID
//		string downloads = Environment.GetEnvironmentVariable("DIRECTORY_DOWNLOADS");
//		Debug.LogWarning(downloads);
//		AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//		AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
//		AndroidJavaObject applicationContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
//		AndroidJavaObject path = applicationContext.Call<AndroidJavaObject>("getFilesDir");
//		string filesPath = path.Call<string>("getCanonicalPath");
//		return filesPath+"../../../../../../Download";
////		return downloads;
//		#endif
		return "/storage/Download";
	}

	public static string GetDownloadFolder () {
//		print (Application.persistentDataPath);
//		return "../../Downloads";
//		string[] temp=(GetAndroidFriendlyFilesPath().Replace ("Android","")).Split (new string[] { "//" },System.StringSplitOptions.None);
//
//		return (temp [0] + "/Download");
		return GetAndroidFriendlyFilesPath();
	}

	public static string ExtractIP(string htmlstring){
		return htmlstring.Split(new string[] {"href=\""}, StringSplitOptions.None)[1].Split(new string[] {"\""},StringSplitOptions.None)[0];
	}

	public static string GetSourceURL(){
		StreamReader reader = new StreamReader(GetDownloadFolder()+"/bluetooth_content_share.html"); 
		string page = reader.ReadToEnd();
		reader.Close();
		return ExtractIP (page)+"/video";
	}

	int m_frameCounter = 0;
	float m_timeCounter = 0.0f;
	float m_lastFramerate = 0.0f;
	public float m_refreshTime = 5f;
	void Update(){

	}




	private Stream stream;
	public void GetVideo(){
		// create HTTP request
		print(sourceURL);
		HttpWebRequest req = (HttpWebRequest) WebRequest.Create(sourceURL+"/video");
		//Optional (if authorization is Digest)
//			req.Credentials = new NetworkCredential("username", "password");
		// get response
		WebResponse resp = req.GetResponse();
		// get response stream
		print(resp.ContentType);
		stream = resp.GetResponseStream();
		StartCoroutine (GetFrame ());
	}

		IEnumerator GetFrame (){
			Byte [] JpegData = new Byte[100000000];

			while(true) {
				int bytesToRead = FindLength(stream);
				print (bytesToRead);
				if (bytesToRead == -1) {
					print("End of stream");
					yield break;
				}

				int leftToRead=bytesToRead;

				while (leftToRead > 0) {
					leftToRead -= stream.Read (JpegData, bytesToRead - leftToRead, leftToRead);
					yield return null;
				}

				MemoryStream ms = new MemoryStream(JpegData, 0, bytesToRead, false, true);

				receivedTexture.LoadImage (ms.GetBuffer ());
				receivedTexture.Apply();
				stream.ReadByte(); // CR after bytes
				stream.ReadByte(); // LF after bytes
			}
		}

		int FindLength(Stream stream)  {
			int b;
			string line="";
			int result=-1;
			bool atEOL=false;

			while ((b=stream.ReadByte())!=-1) {
				if (b==10) continue; // ignore LF char
				if (b==13) { // CR
					if (atEOL) {  // two blank lines means end of header
						stream.ReadByte(); // eat last LF
						return result;
					}
					if (line.StartsWith("Content-Length:")) {
						result=Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
					} else {
						line="";
					}
					atEOL=true;
				} else {
					atEOL=false;
					line+=(char)b;
				}
			}
			return -1;
	}
}