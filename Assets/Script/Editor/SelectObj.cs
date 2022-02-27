using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class SelectGameObject : EditorWindow {
	public class ObjData
	{
		public bool toggleSelcet;
		public bool toggleAssign;

		public Object obj;
		public List<GameObject> objList;

		public Object objBackup;
		public List<GameObject> objListBackup;

		public ObjData ()
		{
			objList = new List<GameObject>();
			objListBackup = new List<GameObject>();
		}
	}

	enum Mode
	{
		Null,
		Empty,
		Select
	}
	Mode mode;

	private Object assignObj;

	private GameObject backUp;

	public bool toggleSelectAll = true;
	public bool toggleAssignAll = false;

	private List<GameObject> rootObj = new List<GameObject>();
	private List<GameObject> emptyPrefab = new List<GameObject>();
	private List<GameObject> rootNew = new List<GameObject> ();

	private List<Object> newPrefab = new List<Object>() ;

	private List<ObjData> objData = new List<ObjData> ();

	private float counting;
	private float totalcount;
	private float progress;

	private Vector2 scrollPosAllPrefab;
	private Vector2 scrollPosEmptyPrefab;


	[MenuItem("Tools/Selection GameObject", false)]
	public static void ShowWindow () {
		GetWindow<SelectGameObject> (false, "Selection GameObject", true);
	}

	void OnGUI ()
	{
		EditorGUILayout.BeginHorizontal ();

		if(GUILayout.Button("Select GameObject",GUILayout.Height(40)))
		{
			rootObj.Clear ();
			rootObj.AddRange(Selection.gameObjects.ToList());
			ListGameObject ();
			mode = Mode.Select;
		}

		if(GUILayout.Button("Select All GameObjects",GUILayout.Height(40)))
		{
			rootObj.Clear ();
			UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (rootObj);
			ListGameObject ();
			mode = Mode.Select;
		}
		if(GUILayout.Button("Select Empty GameObject",GUILayout.Height(40)))
		{
			rootObj.Clear ();
			rootObj.AddRange(Selection.gameObjects.ToList());
			ListGameObject ();
			mode = Mode.Empty;
		}
		if(GUILayout.Button("Select All Empty GameObjects",GUILayout.Height(40)))
		{
			rootObj.Clear ();
			UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (rootObj);
			ListGameObject ();
			mode = Mode.Empty;
		}
		EditorGUILayout.EndHorizontal ();
		if (mode == Mode.Select) {
			DrawSelectGameObject ();
		}
		if (mode == Mode.Empty) {
			DrawSelectEmptyGameObject ();
		}
	}

	void ListGameObject ()
	{
		objData.Clear ();
		newPrefab.Clear ();
		emptyPrefab.Clear ();
		rootNew.Clear ();
		while (rootObj.Count > 0) {
			ObjData objD = new ObjData ();
			GameObject obj =  rootObj [0];
			rootObj.RemoveAt (0);
			Object prefab = PrefabUtility.GetPrefabParent (obj);
			if (CheckMeshAndLOD(obj)) {
				ObjData ob = ContainsObj (prefab);
				if ( ob == null) {
					objD.obj = prefab;
					objD.objList.Add (obj);
					objD.toggleSelcet = true;
					objD.toggleAssign = false;
					objData.Add (objD);
				}
				else
				{
					ob.objList.Add (obj);
				}
				newPrefab.Add (null);
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
	void DrawSelectGameObject ()
	{
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("List All GameObjects "+objData.Count+" Item");

		EditorGUILayout.BeginHorizontal ();

		EditorGUI.BeginChangeCheck();
		toggleSelectAll = GUILayout.Toggle (toggleSelectAll,new GUIContent("","Select All"));
		if (EditorGUI.EndChangeCheck())
		{
			if(toggleSelectAll == true)
			{
				foreach(ObjData k in objData)
				{
					k.toggleSelcet = true;
				}
			}
			else
			{
				foreach(ObjData k in objData)
				{
					k.toggleSelcet = false;
				}
			}
		}

		assignObj = EditorGUILayout.ObjectField (new GUIContent("Assign Object >>>"),(GameObject)assignObj,typeof(GameObject),false);

		EditorGUI.BeginChangeCheck();
		toggleAssignAll = GUILayout.Toggle (toggleAssignAll,new GUIContent("","Assign All"));
		if (EditorGUI.EndChangeCheck())
		{
			if(toggleAssignAll == true)
			{
				foreach(ObjData k in objData)
				{
					k.toggleAssign = true;
				}
			}
			else
			{
				foreach(ObjData k in objData)
				{
					k.toggleAssign = false;
				}
			}
		}

		if(GUILayout.Button("Assign"))
		{
			counting = 0;
			totalcount = objData.Count;
			for(int a = 0 ; a < objData.Count ; a++)
			{
				counting++;
				UpdateProgress ();
				if(objData[a].toggleAssign == true)
				{
					newPrefab [a] = assignObj;
				}
			}
			EditorUtility.ClearProgressBar ();
		}
		if(GUILayout.Button("Clear"))
		{
			assignObj = null;
		}
		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.Space ();

		scrollPosAllPrefab = EditorGUILayout.BeginScrollView (scrollPosAllPrefab);
		EditorGUILayout.BeginVertical ();

		for(int i = 0 ; i < objData.Count ; i++ )
		{
			EditorGUILayout.BeginHorizontal ();

			objData[i].toggleSelcet = GUILayout.Toggle (objData[i].toggleSelcet,new GUIContent("","Selected"));
			EditorGUILayout.ObjectField ((GameObject)objData[i].obj,typeof(GameObject),false);
			newPrefab[i] = (GameObject)EditorGUILayout.ObjectField (newPrefab[i],typeof(GameObject),false);
			objData[i].toggleAssign = GUILayout.Toggle (objData[i].toggleAssign,new GUIContent("","Assign Prefab"));
			if(GUILayout.Button("Replace"))
			{
				List<GameObject> f_back = new List<GameObject>();
				UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (f_back);
				foreach(GameObject b in f_back)
				{
					if(b.name == "BackUp")
					{
						backUp = b;
					}
				}
				if(backUp == null)
				{
					backUp = new GameObject ("BackUp");
					backUp.SetActive (false);
				}
				Replace (i);
			}
			if(GUILayout.Button("Undo"))
			{
				Undo (i);
			}
			EditorGUILayout.EndHorizontal ();

		}
		EditorGUILayout.EndScrollView ();
		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.Space ();

		EditorGUILayout.BeginHorizontal ();
		if(GUILayout.Button("Replace All",GUILayout.Height(40)))
		{
			List<GameObject> f_back = new List<GameObject>();
			UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (f_back);
			foreach(GameObject b in f_back)
			{
				if(b.name == "BackUp")
				{
					backUp = b;
				}
			}
			if(backUp == null)
			{
				backUp = new GameObject ("BackUp");
				backUp.SetActive (false);
			}
			counting = 0;
			totalcount = objData.Count;
			for(int j = 0 ; j < objData.Count ; j++)
			{
				counting++;
				UpdateProgress ();
				if(objData[j].toggleSelcet == true)
				{
					Replace (j);
				}
			}
			EditorUtility.ClearProgressBar ();
		}
		if(GUILayout.Button("Undo All",GUILayout.Height(40)))
		{
			counting = 0;
			totalcount = objData.Count;
			for(int j = 0 ; j < objData.Count ; j++)
			{
				counting++;
				UpdateProgress ();
				if(objData[j].toggleSelcet == true)
				{
					Undo (j);
				}
			}
			EditorUtility.ClearProgressBar ();
		}
		EditorGUILayout.EndHorizontal ();
	}

	void DrawSelectEmptyGameObject ()
	{
		EditorGUILayout.BeginVertical ();
		scrollPosEmptyPrefab = EditorGUILayout.BeginScrollView (scrollPosEmptyPrefab);

		EditorGUILayout.LabelField ("List All Empty GameObjects "+emptyPrefab.Count+" Item");

		foreach(GameObject c in emptyPrefab)
		{
			GameObject emptyPrefab = (GameObject)c;
			EditorGUILayout.ObjectField ((GameObject)emptyPrefab,typeof(GameObject),false);
		}

		EditorGUILayout.EndScrollView ();
		EditorGUILayout.EndVertical ();
	}

	void Replace (int index)
	{
		if (newPrefab [index] == null)
			return;
		ObjData m_obj = objData[index];
		m_obj.objBackup = m_obj.obj;
		m_obj.obj = newPrefab [index];
		m_obj.objListBackup.Clear ();
		m_obj.objListBackup.AddRange (m_obj.objList);
		m_obj.objList.Clear ();
		foreach(GameObject n in m_obj.objListBackup)
		{
			GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab (newPrefab[index]);
			GameObject n_parent = n;
			GameObject p_backup = backUp;
			rootNew.Clear ();

			newObj.transform.parent = n.transform.parent;
			newObj.transform.localPosition = n.transform.localPosition;
			newObj.transform.localRotation = n.transform.localRotation;
			newObj.transform.localScale = n.transform.localScale;
			newObj.transform.SetSiblingIndex(n.transform.GetSiblingIndex());
			m_obj.objList.Add (newObj);

			while(n_parent.transform.parent != null)
			{
				rootNew.Add (n_parent.transform.parent.gameObject);
				n_parent = n_parent.transform.parent.gameObject;
			}
			while(rootNew.Count > 0)
			{
				GameObject rootN = rootNew [rootNew.Count - 1];
				rootNew.RemoveAt (rootNew.Count - 1);
				Transform parent = p_backup.transform.Find (rootN.name);
				if(parent == null)
				{
					rootN = new GameObject (rootN.name);
					rootN.transform.SetParent (p_backup.transform);
					p_backup = rootN;
				}
				else
				{
					p_backup = parent.gameObject;
				}
			}
			n.transform.SetParent (p_backup.transform);

		}
		newPrefab [index] = null;
	}

	void Undo (int index)
	{
		ObjData m_obj = objData[index];
		if(m_obj.objListBackup == null || m_obj.objListBackup.Count == 0)
			return;

		m_obj.obj = m_obj.objBackup;
		m_obj.objBackup = null;
		for(int n = m_obj.objList.Count-1; n >= 0 ; n--)
		{
			m_obj.objListBackup[n].transform.parent = m_obj.objList[n].transform.parent;
			m_obj.objListBackup[n].transform.localPosition = m_obj.objList[n].transform.localPosition;
			m_obj.objListBackup[n].transform.localRotation = m_obj.objList[n].transform.localRotation;
			m_obj.objListBackup[n].transform.localScale = m_obj.objList[n].transform.localScale;
			m_obj.objListBackup[n].transform.SetSiblingIndex(m_obj.objList[n].transform.GetSiblingIndex());
			DestroyImmediate (m_obj.objList[n]);
		}
		m_obj.objList.Clear ();
		m_obj.objList.AddRange (m_obj.objListBackup);
		m_obj.objListBackup.Clear ();
	}

	bool CheckMeshAndLOD (GameObject obj)
	{
		Transform parentObj;
		if(obj.GetComponent<MeshFilter>() == null && obj.GetComponent<LODGroup>() == null)
		{
			return false;
		}
		if(obj.activeInHierarchy == false)
		{
			return false;
			Debug.LogWarning (obj.name+" set active = false",obj);
		}
		if(PrefabUtility.GetPrefabParent(obj) == null)
		{
			Debug.LogWarning (obj.name+" prefab = null",obj);
			return false;
		}
		if(obj.GetComponent<MeshFilter>() != null)
		{
			parentObj = obj.transform.parent;
			if(parentObj != null)
			{
				if(obj.transform.parent.gameObject.GetComponent<LODGroup>() != null)
				{
					return false;
				}
			}

		}
		//		if(obj.GetComponent<LODGroup>() != null)
		//		{
		//			LODGroup gLOD = obj.GetComponent<LODGroup>();
		//			for(int g = 0 ; g < gLOD.GetLODs().Count() ; g++)
		//			{
		//				LOD checkLOD = gLOD.GetLODs () [g];
		//				if(checkLOD.renderers.Length == 0 || checkLOD.renderers[0] == null)
		//				{
		//					Debug.LogWarning (obj.name+" LOD "+g +" = null",obj);
		//					return false;
		//				}
		//			}
		//		}
		return true;
	}

	ObjData ContainsObj (Object o)
	{
		foreach(ObjData n in objData)
		{
			if(n.obj == o)
			{
				return n;
			}
		}
		return null;
	}

	void UpdateProgress()
	{
		progress = (Mathf.InverseLerp (0, totalcount, counting))*100;
		EditorUtility.DisplayProgressBar("Progress... "+(int)progress+" %", "", Mathf.InverseLerp(0,totalcount,counting));
	}
}
