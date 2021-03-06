﻿using UnityEngine;
using System.Collections;
using Juniverse.RPCLibrary;
using Juniverse.Notifications;
using Juniverse.Model;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class ImageGrabber : MonoBehaviour {

    //Key is url
    Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();
    Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

    public void GetIcon(string url, Action<Sprite> callback)
    {
        if (String.IsNullOrEmpty(url))
            return;

        if (Icons.ContainsKey(url))
            callback(Icons[url]);
        else if (callback != null)
            StartCoroutine(DownloadIcon(url, callback));
    }

    public void GetTexture(string url, Action<Texture> callback)
    {
        if (String.IsNullOrEmpty(url))
            return;

        if (Textures.ContainsKey(url))
            callback(Textures[url]);
        else if (callback != null)
            StartCoroutine(DownloadTexture(url, callback));
    }
	
    IEnumerator DownloadIcon(string url, Action<Sprite> callback)
    {
        WWW www = new WWW(url);
        yield return www;
        if (www.error != null)
            callback(null);
        else
        {
            Rect rec = new Rect(0, 0, www.texture.width, www.texture.height);
            Sprite icon = Sprite.Create(www.texture, rec, new Vector2(0.5f, 0.5f), 100);
            if (icon != null)
            {
                Icons[url] = icon;
            }
            callback(icon);
        }
    }

    IEnumerator DownloadTexture(string url, Action<Texture> callback)
    {
        WWW www = new WWW(url);
        yield return www;
        if (www.error != null)
            callback(null);
        else
            callback(www.texture);
    }
}
