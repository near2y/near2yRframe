using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

public class WriteReadData
{
    public static void CreateXML<T>(string xmlPath,T data)
    {
        if (File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter streamWriter = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xmlSerializer = new XmlSerializer(data.GetType());
        xmlSerializer.Serialize(streamWriter, data);
        streamWriter.Close();
        fileStream.Close();
    }

    public static void CreateBinary<T>(string binaryPath,T data)
    {
        if (File.Exists(binaryPath)) File.Delete(binaryPath);
        FileStream fileStream = new FileStream(binaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        binaryFormatter.Serialize(fileStream, data);
        fileStream.Close();
    }

    public static T ReadBinary<T>(TextAsset textAsset)
    {
        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        T rtn = (T)bf.Deserialize(stream);
        stream.Close();
        return rtn;
    }

}
