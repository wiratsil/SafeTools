using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GetGameObject : EditorWindow	 {
	public GameObject backupPrefabs = null;

	private static List<GameObject> rootObj = new List<GameObject>();
	private static List<GameObject> rootNewObj = new List<GameObject>();
	private static List<GameObject> allPrefab = new List<GameObject>();
	private static List<GameObject> newPrefab = new List<GameObject>() ;
	private static List<GameObject> listBackup = new List<GameObject> ();
	private static List<GameObject> listShow = new List<GameObject>();
	private static List<GameObject> backupListShow = new List<GameObject>();
	private static List<GameObject> emptyPrefab = new List<GameObject>();

	private static List<List<GameObject>> listObjOfPrefab = new List<List<GameObject>> ();
	private static List<List<GameObject>> backuplistObjOfPrefab = new List<List<GameObject>> ();



	private Vector2 scrollPosAllPrefab;
	private Vector2 scrollPosEmptyPrefab;

	private Texture2D prefabSelectionTexture;
	private Texture2D prefabEmptyTexture;

	private Color prefabSelectionColor = new Color (0f/255f,230f/255f,0f/255f,1f);
	private Color prefabEmptyColor = new Color (255f/255f,51f/255f,51f/255f,1f);

	private	Rect prefabSelection;
	private	Rect prefabEmpty;


	[MenuItem("Tools/List All GameObjects", false)]
	private static void ListPrefab () {
		GetWindow<GetGameObject> (false,"List GameObjects",true);

		rootObj.Clear ();
		allPrefab.Clear ();
		newPrefab.Clear ();
		listBackup.Clear ();
		listShow.Clear ();
		listObjOfPrefab.Clear ();
		backuplistObjOfPrefab.Clear ();
		emptyPrefab.Clear ();
		UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (rootObj);

		while (rootObj.Count > 0) {
			GameObject obj =  rootObj [0];
			LODGroup lodCheck = null;
			MeshRenderer meshCheck = null;
			rootObj.RemoveAt (0);

			lodCheck = obj.GetComponent<LODGroup> ();
			meshCheck = obj.GetComponent<MeshRenderer> ();
			if(obj.name == "BackUpPrefabs")
			{
				continue;
			}
			var prefab = PrefabUtility.GetPrefabParent (obj);
			if (prefab == null) {
				if (lodCheck == null) {
					if (meshCheck != null) {
						allPrefab.Add (obj);
						listBackup.Add (null);
					}
					else
					{
						Component[]  checkCom;
						checkCom = obj.GetComponents(typeof(Component));
						if(checkCom.Length <= 1 && obj.transform.childCount == 0)
						{
							emptyPrefab.Add (obj);
						}

						for (int i = 0; i < obj.transform.childCount; i++) {
							rootObj.Add (obj.transform.GetChild (i).gameObject);
						}
					}
				}
			}

			else
			{
				listBackup.Add (null);
				allPrefab.Add (obj);
			}
		}

		foreach(GameObject j in allPrefab)
		{
			bool checkList = false;
			var checkPrefab = PrefabUtility.GetPrefabParent (j);
			foreach(GameObject e in listShow)
			{
				var checkListPrefab = PrefabUtility.GetPrefabParent (e);
				if (checkListPrefab == null || checkPrefab == null) {
					continue;
				}

				if (checkListPrefab.name == checkPrefab.name) {
					checkList = true;
				}

			}
			if(checkList == false)
			{
				listShow.Add(j);
			}

		}

		for(int h = 0 ; h < listShow.Count ; h++)
		{
			List<GameObject> listObj = new List<GameObject> ();
			List<GameObject> backupListObj = new List<GameObject> ();
			listObjOfPrefab.Add (listObj);
			backuplistObjOfPrefab.Add (backupListObj);
			foreach(GameObject e in allPrefab)
			{
				var m_ListShow = PrefabUtility.GetPrefabParent (listShow[h]);
				var m_AllPrefab = PrefabUtility.GetPrefabParent (e);
				if(m_ListShow == null || m_AllPrefab == null)
				{
					continue;
				}

				if(m_ListShow.name == m_AllPrefab.name)
				{
					listObj.Add (e);
					backupListObj.Add (null);
				}
			}
		}

	}

	void OnGUI ()
	{

		DrawLayout ();
		DrawListEmptyPrefab ();
		DrawListAllPrefabs ();
	}

	void OnEnable ()
	{
		InitTextures ();
	}

	void InitTextures()
	{
		prefabSelectionTexture = new Texture2D (1, 1);
		prefabSelectionTexture.SetPixel (0,0,prefabSelectionColor);
		prefabSelectionTexture.Apply ();

		prefabEmptyTexture = new Texture2D (1, 1);
		prefabEmptyTexture.SetPixel (0,0,prefabEmptyColor);
		prefabEmptyTexture.Apply ();
	}

	void DrawLayout ()
	{

		prefabSelection.x = 0;
		prefabSelection.y = 0;
		prefabSelection.width = Screen.width/1.5f;
		prefabSelection.height = Screen.height;

		prefabEmpty.x = prefabSelection.width;
		prefabEmpty.y = 0;
		prefabEmpty.width = Screen.width/3f;
		prefabEmpty.height = Screen.height;

		GUI.DrawTexture (prefabSelection,prefabSelectionTexture);
		GUI.DrawTexture (prefabEmpty,prefabEmptyTexture);
	}


	void DrawListEmptyPrefab ()
	{
		GUILayout.BeginArea (prefabEmpty);
		EditorGUILayout.BeginVertical ();
		scrollPosEmptyPrefab = EditorGUILayout.BeginScrollView (scrollPosEmptyPrefab,GUILayout.Width(250),GUILayout.Height(380));

		EditorGUILayout.LabelField ("List All Empty GameObjects "+emptyPrefab.Count+" Item");

				foreach(GameObject c in emptyPrefab)
				{
					GameObject emptyPrefab = (GameObject)c;
					EditorGUILayout.ObjectField ((GameObject)emptyPrefab,typeof(GameObject),false);
				}

		EditorGUILayout.EndScrollView ();
		EditorGUILayout.EndVertical ();
		GUILayout.EndArea ();
	}

	void DrawListAllPrefabs ()
	{
		GUILayout.BeginArea (prefabSelection);

		EditorGUILayout.BeginVertical ();
		scrollPosAllPrefab = EditorGUILayout.BeginScrollView (scrollPosAllPrefab, GUILayout.Width(500), GUILayout.Height(300));

		EditorGUILayout.LabelField ("List All GameObjects "+listShow.Count+" Item");

		//		Dictionary< GameObject,Object>.KeyCollection keys =  dicPrefabs.Keys;
		//		GameObject[] keyObjs = new GameObject[keys.Count];
		//		keys.CopyTo (keyObjs, 0);
		//		GameObject c = (GameObject)dicPrefabs[keyObjs[i]];

		for(int i = 0 ; i < listShow.Count ; i++ )
		{
			EditorGUILayout.BeginHorizontal ();
			GameObject selectPrefab = (GameObject)listShow[i];
			EditorGUILayout.ObjectField ((GameObject)selectPrefab,typeof(GameObject),false);
			newPrefab.Add (null);
			backupListShow.Add (null);
			newPrefab[i] = (GameObject)EditorGUILayout.ObjectField (newPrefab[i],typeof(GameObject),false);
			if (GUILayout.Button ("Replace") && newPrefab[i] != null) {

				ReplaceButton (i,selectPrefab);
			}
			if (GUILayout.Button ("Undo") && backupListShow[i] != null) {

				UndoButton (i);
			}
			EditorGUILayout.EndHorizontal ();

		}

		EditorGUILayout.EndScrollView ();
		EditorGUILayout.EndVertical ();

		if (GUILayout.Button ("Replace All",GUILayout.Height(40))) {
			for(int i = 0 ; i < listShow.Count ; i++ )
			{
				if(newPrefab[i] == null)
				{
					continue;
				}
				GameObject selectPrefab = (GameObject)listShow[i];
				ReplaceButton (i,selectPrefab);
			}
		}
		if (GUILayout.Button ("Undo All",GUILayout.Height(40))) {
			for(int i = 0 ; i < backupListShow.Count ; i++ )
			{
				if(backupListShow[i] == null)
				{
					continue;
				}
				UndoButton (i);
			}
		}
		GUILayout.EndArea ();
	}

	void ReplaceButton (int i,GameObject selectPrefab)
	{

		GameObject newObj;
		List<GameObject> m_ListObj = new List<GameObject> ();
		List<GameObject> m_BackupListObj = new List<GameObject> ();
		m_ListObj = listObjOfPrefab [i];
		m_BackupListObj = backuplistObjOfPrefab[i];

		var prefabType = PrefabUtility.GetPrefabType (newPrefab[i]);

		backupListShow [i] = listShow [i];
		listShow [i] = null;

		for(int p = 0 ; p < m_ListObj.Count; p++)
		{
			GameObject a_Prefab =  m_ListObj [p];

			rootNewObj.Clear ();
			GameObject checkBackup = null;
			GameObject child;
			GameObject parent;
			GameObject checkParent;
			checkBackup = GameObject.Find ("BackUpPrefabs");
			if (checkBackup == null) {
				backupPrefabs = new GameObject ("BackUpPrefabs");
			} else {
				backupPrefabs = checkBackup;
			}

			if (prefabType == PrefabType.Prefab) {

				newObj = (GameObject)PrefabUtility.InstantiatePrefab (newPrefab [i]);


			}
			else
			{
				Debug.Log ("Prefab Replace Error");
				EditorGUILayout.EndHorizontal ();
				break;
			}

			newObj.transform.parent = a_Prefab.transform.parent;
			newObj.transform.localPosition = a_Prefab.transform.localPosition;
			newObj.transform.localRotation = a_Prefab.transform.localRotation;
			newObj.transform.localScale = a_Prefab.transform.localScale;
			newObj.transform.SetSiblingIndex(a_Prefab.transform.GetSiblingIndex());
			if(m_BackupListObj [p] != null)
			{
				DestroyImmediate (m_BackupListObj [p]);
			}
			m_BackupListObj[p] = a_Prefab;
			m_ListObj [p] = null;
			m_ListObj [p] = newObj;
			checkParent = a_Prefab;
			if(listShow[i] == null)
			{
				listShow [i] = newObj;
			}
			if (checkParent.transform.parent != null) {
				checkParent = checkParent.transform.parent.gameObject;
				while (checkParent != null)
				{
					child = a_Prefab;
					parent = Instantiate (checkParent.gameObject);
					parent.name = checkParent.gameObject.name;
					while (parent.transform.childCount > 0) {
						DestroyImmediate(parent.transform.GetChild (0).gameObject) ;
					}
					child.transform.SetParent (parent.transform);
					a_Prefab = parent;
					rootNewObj.Add (a_Prefab);

					if (checkParent.transform.parent != null) {
						checkParent = checkParent.transform.parent.gameObject;
					}
					else
					{
						checkParent = null;
					}

				}
			}
			GameObject backupObj = backupPrefabs;
			GameObject childBackup = null;
			GameObject desParen = null;

			if (backupPrefabs.transform.childCount > 0) {
				while (rootNewObj.Count > 0) {
					GameObject m_rootNewObj = rootNewObj [rootNewObj.Count - 1];
					rootNewObj.RemoveAt (rootNewObj.Count - 1);

					for (int w = 0; w < backupObj.transform.childCount; w++) {
						childBackup = backupObj.transform.GetChild (w).gameObject;
						if (m_rootNewObj.name == childBackup.name) {
							if (desParen == null) {
								desParen = a_Prefab;
							}
							a_Prefab = a_Prefab.transform.GetChild (0).gameObject;
							backupObj = backupObj.transform.GetChild (w).gameObject;

						}
					}

				}
				a_Prefab.transform.SetParent (backupObj.transform);
				DestroyImmediate (desParen);
			}
			else
			{
				a_Prefab.transform.SetParent (backupPrefabs.transform);
			}

		}


		newPrefab [i] = null;
		Repaint ();
	}

	void UndoButton (int i)
	{
		GameObject oldObj;
		List<GameObject> m_ListObj = new List<GameObject> ();
		List<GameObject> m_BackupListObj = new List<GameObject> ();
		m_ListObj = listObjOfPrefab [i];
		m_BackupListObj = backuplistObjOfPrefab[i];


		for (int b = 0; b < m_BackupListObj.Count; b++) {
			GameObject b_AllPrefab = m_ListObj [b];
			GameObject b_BackupPrefab = m_BackupListObj[b];

			oldObj = b_BackupPrefab;
			GameObject desNewObj = b_AllPrefab;

			if (b_BackupPrefab.transform.parent.childCount <= 1 && b_BackupPrefab.transform.parent.name != "BackUpPrefabs") {
				while (oldObj.transform.parent.name != "BackUpPrefabs"  && oldObj.transform.parent.childCount <= 1) {
					oldObj = oldObj.transform.parent.gameObject;
				}
				b_BackupPrefab.transform.parent = b_AllPrefab.transform.parent;
				b_BackupPrefab.transform.localPosition = b_AllPrefab.transform.localPosition;
				b_BackupPrefab.transform.localRotation = b_AllPrefab.transform.localRotation;
				b_BackupPrefab.transform.localScale = b_AllPrefab.transform.localScale;
				b_BackupPrefab.transform.SetSiblingIndex (b_AllPrefab.transform.GetSiblingIndex ());
				m_ListObj [b] = b_BackupPrefab;
				m_BackupListObj[b] = null;
				DestroyImmediate (oldObj.gameObject);
			}
			else
			{
				b_BackupPrefab.transform.parent = b_AllPrefab.transform.parent;
				b_BackupPrefab.transform.localPosition = b_AllPrefab.transform.localPosition;
				b_BackupPrefab.transform.localRotation = b_AllPrefab.transform.localRotation;
				b_BackupPrefab.transform.localScale = b_AllPrefab.transform.localScale;
				b_BackupPrefab.transform.SetSiblingIndex (b_AllPrefab.transform.GetSiblingIndex ());
				m_ListObj [b] = b_BackupPrefab;
				m_BackupListObj[b] = null;
			}


			DestroyImmediate (desNewObj);
			EditorGUILayout.EndHorizontal ();


		}

		listShow [i] = backupListShow [i];
		backupListShow [i] = null;

	}


}
