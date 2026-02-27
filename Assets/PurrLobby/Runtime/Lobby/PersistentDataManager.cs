using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/*
* @brief  Contains class declaration for PersistentDataManager
* @details Script that will save and load the persistent game data in a local file
*/
public class PersistentDataManager : MonoBehaviour {

	public void ChangeUsername(string _username)
	{
        SaveFile(_username);
	}

    public string LoadUsername()
    {
        return LoadFile();
    }

	public void SaveFile(string _username)
	{
		string destination = Application.persistentDataPath + "/save.dat";
		FileStream file;

		if(File.Exists(destination)) file = File.OpenWrite(destination);
		else file = File.Create(destination);

		PersistentGameData data = new PersistentGameData(_username);
		BinaryFormatter bf = new BinaryFormatter();
		bf.Serialize(file, data);
		file.Close();
	}

	public string LoadFile()
	{
		string destination = Application.persistentDataPath + "/save.dat";
		FileStream file;

		if(File.Exists(destination)) file = File.OpenRead(destination);
		else
		{
			return "Player";
		}

		BinaryFormatter bf = new BinaryFormatter();
		PersistentGameData data = (PersistentGameData) bf.Deserialize(file);
		file.Close();

		return data.m_username;
	}

}