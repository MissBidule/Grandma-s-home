[System.Serializable]
/*
* @brief  Contains class declaration for PersistentGameData
* @details Script that contains all data written in a file to not give them back each time we start the app
*/
public class PersistentGameData
{
	public string m_username;

	public PersistentGameData(string _username)
	{
		m_username = _username;
	}
}