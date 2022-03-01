using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FileTool
{
    public static string ReadFile(string path)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(bytes);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"读取文件失败:{e.Message}");
            return "";
        }
    }

    public static void WriteFile(string path, string content)
    {
        try
        {
            byte[] myBytes = Encoding.UTF8.GetBytes(content);
            using (FileStream file = new FileStream(path, FileMode.OpenOrCreate))
            {
                file.Write(myBytes, 0, myBytes.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"写入文件失败:{e.Message}");
        }
    }
    public static string LoadTxtFileByWWW(string srcPath)
    {
        string path = Application.platform == RuntimePlatform.Android ? srcPath : string.Format("file://{0}", srcPath);
        UnityWebRequest www = new UnityWebRequest(path);
        www.timeout = 10;
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SendWebRequest();
        while (!www.isDone)
        {
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError($"loadPath:{path} error:{www.error}");
            return "";
        }
        return www.downloadHandler.text;
    }
}