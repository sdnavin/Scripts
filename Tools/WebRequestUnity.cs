using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.IO; 
using System.Net;
using System.Text; 
using JsonFx.Json;
using System.Security.Cryptography;
using System.Threading;
using RestSharp;

public class WebRequestUnity :MonoBehaviour {

	public static IEnumerator makerequest(string args,System.Action<string> result){
		IRestResponse mydata=null;
		RestClient rc = new RestClient (CandoWebLinks.mainAPI);
		RestRequest rr = new RestRequest (args);
			rc.ExecuteAsync (rr, response =>{ mydata = response;});
		if (result != null) {
			while(mydata==null) {
				yield return null;
			}
			if (mydata!=null) {
				result (string.IsNullOrEmpty (mydata.ErrorMessage) ? mydata.Content : mydata.ErrorMessage);
			}
		}
	}
	public static IEnumerator makerequest(string args,Method methodtodo,System.Action<string> result){
		IRestResponse mydata=null;
		RestClient rc = new RestClient (CandoWebLinks.mainAPI);
		RestRequest rr = new RestRequest (args,methodtodo);
			rc.ExecuteAsync (rr, response =>{ mydata = response;});
		if (result != null) {
			while(mydata==null) {
				yield return null;
			}
			if (mydata!=null) {
				result (string.IsNullOrEmpty (mydata.ErrorMessage) ? mydata.Content : mydata.ErrorMessage);
			}
		}
	}
	public static IEnumerator makerequest(string args,Method methodtodo,string Pdata,System.Action<string> result){
		print ("D :" + JsonWriter.Serialize(JsonReader.Deserialize (Pdata)));

		float tyms = Time.time;
		float waittym = 10;
		IRestResponse mydata=null;
		RestClient rc = new RestClient (CandoWebLinks.mainAPI);
		RestRequest rr = new RestRequest (args, methodtodo);
		if (!string.IsNullOrEmpty (Pdata)) {
			rr.AddHeader ("content-length", "application/json");
			rr.AddHeader ("content-type", "" + Pdata.Length);
			rr.Parameters.Clear ();
			rr.RequestFormat = DataFormat.Json; 
			rr.AddJsonBody (JsonReader.Deserialize (Pdata));
		}
			rc.ExecuteAsync (rr, response =>{ mydata = response;});
		if (result != null) {
			while(((tyms+waittym)>Time.time)&&mydata==null) {
				//				print (Time.time);
				yield return null;
			}
			if (mydata!=null) {
				result (string.IsNullOrEmpty (mydata.ErrorMessage) ? mydata.Content : mydata.ErrorMessage);
			}else if (mydata==null){
				result ("error");
			}
		}
	}
	void Update(){
	}
}



