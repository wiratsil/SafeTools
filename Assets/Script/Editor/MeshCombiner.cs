using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;

[ExecuteInEditMode]
public class MeshCombiner : EditorWindow
{
	public class SubMesh
	{
		public List<Transform> subTransforms;
		public List<Mesh> subMeshes;

		public SubMesh (List<Transform> k, List<Mesh> v)
		{
			subTransforms = k;
			subMeshes = v;
		}
	}

	public class MergeData
	{
		public List<List<GameObject>> _objectLists;
		public List<List<SubMesh>> _subMeshLists;
		public List<List<Material>> _matLists;
		public List<Transform> _tranMeshColLists;
		public List<Mesh> _meshColLists;


		public MergeData ()
		{
			_objectLists = new List<List<GameObject>>();
			_subMeshLists = new List<List<SubMesh>>();
			_matLists = new List<List<Material>>();
			_tranMeshColLists = new List<Transform>();
			_meshColLists = new List<Mesh>();
		}
		public MergeData (List<List<GameObject>> o,List<List<SubMesh>> s,List<List<Material>> m, List<Transform> t, List<Mesh> me)
		{
			_objectLists = o;
			_subMeshLists = s;
			_matLists = m;
			_tranMeshColLists = t;
			_meshColLists = me;
		}
	}

	public enum Mode
	{
		patchChild,
		select,
		searchAndMerge,
		fullAuto
	}

	[MenuItem("Fishing Simulator/MeshCombiner")]
	public static void ShowWindow ()
	{
		MeshCombiner window = (MeshCombiner)EditorWindow.GetWindow(typeof(MeshCombiner), false, "Mesh Combiner");
		window.Show();
	}

	public string meshContainsName;
	public float selectionRadius = 1;
	public List<SubMesh> subMeshList = new List<SubMesh>();
	public Mode mode;
	private GameObject parent;
	private Vector2 scrollPos = new Vector2(0, 0);
	private Vector2 scrollPos2 = new Vector2(0, 0);
	private List<Material> sharedMats = new List<Material>();
	int polyCount = 0;
	int fileCount = 0;
	int mergeCount = 0;
	int polyMergeCount = 0;
	int groupCount = 0;
	int foundCount = 0;
	string objectsToString = "";
	string scenePath = "";
	string filePath = "";
	bool canMerge = false;
	bool lodGroup = true;
	float radius = 10;
	float counting;
	float totalcount;
	float progress;

	private string textSeObj ;
	List<List<GameObject>> objectLists = new List<List<GameObject>>();
	List<List<SubMesh>> subMeshLists = new List<List<SubMesh>>();
	List<List<Material>> matLists = new List<List<Material>>();
	public List<Transform> tranMeshColLists = new List<Transform>();
	public List<Mesh> meshColLists = new List<Mesh>();
	List<GameObject> sg = new List<GameObject>();
	List<GameObject> sel = new List<GameObject>();
	List<GameObject> lodList = new List<GameObject> ();
	List<GameObject> selectedObjs = new List<GameObject>();
	List<bool> showList = new List<bool>();

	Dictionary<string,List<MergeData>> dataDic = new Dictionary<string, List<MergeData>> ();
	Dictionary<string,List<GameObject>> seObj = new Dictionary<string, List<GameObject>> ();
	Dictionary<string,List<float>> lodRTH = new Dictionary<string, List<float>> ();
	Dictionary<string,int> polyMergeCountDic = new Dictionary<string, int>();

	List<Mesh> meshes = new List<Mesh>();
	MeshFilter mf;

	GameObject draw;
	DrawGiz _drawGiz;

	void Update ()
	{
		if (mode == Mode.searchAndMerge) {
			if (Selection.gameObjects.Length == 1) {
				if (CheckMeshAndLOD (Selection.gameObjects [0])) {
					if (draw == null) {
						draw = Selection.gameObjects [0];
						_drawGiz = draw.AddComponent<DrawGiz> ();
					} else if (draw.GetInstanceID () != Selection.gameObjects [0].GetInstanceID ()) {
						DestroyImmediate (draw.GetComponent<DrawGiz> ());
						draw = Selection.gameObjects [0];
						_drawGiz = draw.AddComponent<DrawGiz> ();
					}
				} else if (draw != null && draw.GetComponent<DrawGiz> () != null) {
					DestroyImmediate (draw.GetComponent<DrawGiz> ());
					draw = null;
				}
				if (_drawGiz != null) {
					_drawGiz.radius = radius;
				}
			} else if (draw != null && draw.GetComponent<DrawGiz> () != null) {
				DestroyImmediate (draw.GetComponent<DrawGiz> ());
				draw = null;
			}
		}
		else if(draw != null && draw.GetComponent<DrawGiz> () != null)
		{
			DestroyImmediate (draw.GetComponent<DrawGiz> ());
			draw = null;
		}
	}

	void OnGUI ()
	{
		EditorGUI.BeginChangeCheck ();
		mode = (Mode)EditorGUILayout.EnumPopup("Select Merge Mode", mode);
		if (EditorGUI.EndChangeCheck ())
		{
			lodRTH.Clear ();
			polyMergeCountDic.Clear() ;
			mergeCount = 0;
			groupCount = 0;
			foundCount = 0;
			objectsToString = "";
			dataDic.Clear ();
			seObj.Clear ();
			sg.Clear ();
			sel.Clear ();
			showList.Clear ();
			canMerge = false;
		}
		if (mode == Mode.select || mode == Mode.patchChild || mode == Mode.searchAndMerge || mode == Mode.fullAuto) {

			if( mode == Mode.searchAndMerge)
			{
				EditorGUILayout.LabelField ("Please Chose a Gameobject");
				radius = float.Parse( EditorGUILayout.TextField ("Search Radius", radius.ToString()));
			}
			if( mode == Mode.fullAuto)
			{
				radius = float.Parse( EditorGUILayout.TextField ("Group Radius", radius.ToString()));
			}


			if (GUILayout.Button("Select"))
			{
				totalcount = 0;
				counting = 0;
				UpdateProgress ();
				lodRTH.Clear ();
				polyMergeCountDic.Clear() ;
				mergeCount = 0;
				groupCount = 0;
				foundCount = 0;
				objectsToString = "";
				dataDic.Clear ();
				seObj.Clear ();
				sg.Clear ();
				sel.Clear ();
				showList.Clear ();
				if(mode == Mode.select)
				{
					sel = Selection.gameObjects.ToList();
					foreach(GameObject _sel in sel)
					{
						if(CheckMeshAndLOD(_sel))
						{
							sg.Add (_sel);
						}
					}
				}
				if(mode == Mode.patchChild)
				{
					sel = Selection.gameObjects.ToList();
					if (sel.Count > 1) {
						Selection.objects = sg.ToArray ();
						canMerge = false;
						Debug.LogError ("Please Select Only One Patch");
						return;
					}
					else if (sel[0].GetComponent<MeshFilter>() != null || sel[0].GetComponent<LODGroup>() != null)
					{
						Selection.objects = sg.ToArray ();
						canMerge = false;
						Debug.LogError ("This not patch child");
						return;
					}
					Patchchild (sel[0]);
				}
				if(mode == Mode.searchAndMerge)
				{
					if(draw == null)
					{
						Debug.LogWarning ("Please Chose a GameObject");
						return;
					}
					else if(Selection.objects.Length > 1)
					{
						Debug.LogWarning ("Please Chose Only One Object");
						return;
					}
					Searching ();
				}
				if(mode == Mode.fullAuto)
				{
					mode = Mode.fullAuto;
					FullAuto ();
				}
				polyCount = 0;
				if(mode == Mode.select || mode == Mode.searchAndMerge)
				{
					if(sg.Count <= 1)
					{
						canMerge = false;
						Debug.LogError ("Not Found Gameobject For Merge");
						return;
					}
					sg = sg.OrderBy (go => go.name).ToList();
					while(sg.Count > 0)
					{
						bool added = false;
						int num = 0;
						Object m_sgPrefab;
						Object c_sgPrefab;
						GameObject m_sg = sg [0];
						sg.RemoveAt (0);
						m_sgPrefab = PrefabUtility.GetPrefabParent (m_sg);

						foreach(string k_se in seObj.Keys)
						{
							c_sgPrefab = PrefabUtility.GetPrefabParent (seObj[k_se][0]);
							if(m_sgPrefab == c_sgPrefab)
							{
								seObj [k_se].Add (m_sg);
								added = true;
							}
						}
						if(added == false)
						{
							while(seObj.ContainsKey(m_sgPrefab.name+"_Group_"+num))
							{ num++; }
							List<GameObject> newObjList = new List<GameObject> ();
							newObjList.Add (m_sg);
							seObj.Add (m_sgPrefab.name+"_Group_"+num,newObjList);
						}
					}
				}
				sg.Clear ();
				totalcount = seObj.Count;
				foreach (string key in seObj.Keys)
				{
					counting++;
					UpdateProgress ();
					if (seObj [key].Count <= 1) {
						continue;
					}
					else
					{

						groupCount ++;
						foundCount += seObj [key].Count;
						sg.AddRange(seObj[key]);
					}
					List<GameObject> gos = seObj [key];
//					gos = gos.OrderBy (go => go.name).ToList();
					int lodCount = 1;
					if(gos[0].GetComponent<LODGroup>() != null)
					{
						lodCount = gos[0].GetComponent<LODGroup>().GetLODs().Count();
						if(!lodRTH.ContainsKey(key))
						{
							lodRTH.Add (key,new List<float>());
							for(int t = 0 ; t< lodCount ; t ++)
							{
								float rth = gos [0].GetComponent<LODGroup> ().GetLODs () [t].screenRelativeTransitionHeight;
								GameObject gg = gos [0].GetComponent<LODGroup> ().GetLODs () [t].renderers [0].gameObject;
								if (gg.GetComponent<BillboardRenderer> () != null)
								{
									lodRTH[key][lodRTH[key].Count-1] = rth ;
									Debug.LogWarning (gg.name+" LOD "+t+" have BilboardRenderer");
								}
								else
								{
									lodRTH[key].Add(rth);
								}
							}
						}

					}
					for(int h = 0 ; h < lodCount ; h ++ )
					{
						meshes.Clear();
						selectedObjs.Clear();
						polyCount = 0;
						MergeData md = new MergeData ();
						objectLists = md._objectLists;
						subMeshLists = md._subMeshLists;
						matLists = md._matLists;
						tranMeshColLists = md._tranMeshColLists;
						meshColLists = md._meshColLists;


						if (dataDic.ContainsKey (key)) {
							dataDic [key].Add (md);
						}
						else
						{
							List<MergeData> mds = new List<MergeData> ();
							mds.Add (md);
							dataDic.Add (key,mds);
						}

						for (int i = 0; i < gos.Count; i++)
						{

							LODGroup lod = gos[i].GetComponent<LODGroup>();
							GameObject go = null;
							if (lod != null) {
								go = lod.GetLODs () [h].renderers [0].gameObject;
								MeshCollider meshCol = go.GetComponent<MeshCollider> ();
								if (meshCol != null) {
									tranMeshColLists.Add (gos [i].transform);
									meshColLists.Add (meshCol.sharedMesh);
								}
							}
							else
							{
								go = gos [i];
								MeshCollider meshCol = go.GetComponent<MeshCollider> ();
								if (meshCol != null) {
									tranMeshColLists.Add (gos [i].transform);
									meshColLists.Add (meshCol.sharedMesh);
								}
							}
							if (go.GetComponent<BillboardRenderer>() != null)
							{
								continue;
							}
							if (go.GetComponent<MeshFilter>() == null)
							{
								Selection.activeGameObject = go;
								Debug.LogError(go.name + " don't have mesh data!");
								return;
							}
							sharedMats = go.GetComponent<Renderer>().sharedMaterials.ToList();

							int id = 0;

							if (matLists.Count == 0)
							{
								objectLists.Add(new List<GameObject>());
								matLists.Add(sharedMats);
							}
							for (int j = 0; j < matLists.Count; j++)
							{
								if (CompareList(sharedMats, matLists[j]))
								{
									id = j;
									break;
								}
								else if (j == matLists.Count - 1)
								{
									id = j + 1;
									objectLists.Add(new List<GameObject>());
									matLists.Add(sharedMats);
								}
							}

							objectLists[id].Add(go);
						}
						for (int i = 0; i < matLists.Count; i++)
						{
							List<MeshFilter> mfs = new List<MeshFilter>();

							for (int j = 0; j < objectLists[i].Count; j++)
							{
								mfs.Add(objectLists[i][j].GetComponent<MeshFilter>());
								polyCount += mfs[j].sharedMesh.vertexCount;
							}

							if (mfs[0].sharedMesh.subMeshCount > 0)
								subMeshLists.Add(SubMeshToMesh(mfs));
							if (polyCount > 65000) {
								objectsToString += meshContainsName + "_" + objectLists[i][0].name + "_" + matLists[i][0].name + " : " + polyCount+" ***Poly > 65000***"  + "\n";
							}
							else
							{
								objectsToString += meshContainsName + "_" + objectLists[i][0].name + "_" + matLists[i][0].name + " : " + polyCount + "\n";
							}

							mergeCount++;
							polyMergeCount += polyCount;
							polyCount = 0;
						}

						if(!polyMergeCountDic.ContainsKey(key))
						{
							polyMergeCountDic.Add (key,polyMergeCount);
						}
						polyMergeCount = 0;
					}
					objectsToString += "\n";
				}
				Selection.objects = sg.ToArray();
				canMerge = true;
				foreach(string p in polyMergeCountDic.Keys)
				{
					if(polyMergeCountDic[p]>65000)
					{
						canMerge = false;
					}
				}

			}
			EditorUtility.ClearProgressBar ();

			lodGroup = EditorGUILayout.Toggle (new GUIContent("LOD Group","Add Component LOD Group in new object"),lodGroup);

			if(mode == Mode.searchAndMerge || mode == Mode.fullAuto)
			{
				EditorGUILayout.LabelField("Found Count : " + foundCount);
			}
			EditorGUILayout.LabelField("Merge Count : " + mergeCount);


			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			if(mode == Mode.searchAndMerge || mode == Mode.select || mode == Mode.patchChild)
			{
				EditorGUILayout.TextArea(objectsToString);
			}

			else if(mode == Mode.fullAuto)
			{
				EditorGUILayout.Space ();
				EditorGUILayout.LabelField ("Select Merge Group");
				List<string> seObjKeyList = new List<string> ();
				seObjKeyList = seObj.Keys.ToList ();
				int numOfseObjList = 0;
				foreach(KeyValuePair<string,List<GameObject>> sDic in seObj )
				{
					if (sDic.Value.Count <= 1) {
						continue;
					}
					EditorGUILayout.BeginHorizontal ();
					showList.Add (true);
					showList [numOfseObjList] = EditorGUILayout.Toggle (showList[numOfseObjList],GUILayout.MaxWidth(10));
					showList [numOfseObjList] = EditorGUILayout.Foldout (showList[numOfseObjList],new GUIContent(sDic.Key,"Selected for merge"));
					EditorGUILayout.EndHorizontal ();
					if(showList [numOfseObjList])
					{
						foreach(GameObject gObj in sDic.Value)
						{
							EditorGUILayout.ObjectField ((GameObject)gObj,typeof(GameObject),false,GUILayout.MaxWidth(400));
						}
						EditorGUILayout.Space ();

					}
					numOfseObjList++;
				}

			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.LabelField("Group Count : "+ groupCount);
			meshContainsName = EditorGUILayout.TextField("Name of merged object", meshContainsName);

			EditorGUI.BeginDisabledGroup (canMerge == false);
			if (GUILayout.Button ("Marge")) {
				Mesh ();
			}
			EditorUtility.ClearProgressBar ();
			EditorGUI.EndDisabledGroup ();

			EditorGUILayout.Space ();

		}

	}

	void Mesh ()
	{
		counting = 0;
		canMerge = false;
		int numOfseObjList = 0;
		totalcount = dataDic.Count;
		foreach (string key in dataDic.Keys)
		{
			counting++;
			UpdateProgress ();
			if(showList.Count > 0 && showList[numOfseObjList] == false)
			{
				numOfseObjList++;
				continue;
			}
			numOfseObjList++;
			if(polyMergeCountDic[key] > 65000)
			{
				continue;
			}
			NewFolder (meshContainsName, key);
			LOD[] lods = new LOD[dataDic[key].Count] ;
			int lodCount = 0;
			int merCoung = 0;
			foreach(MergeData m in dataDic[key])
			{
				//Mesh
				for (int i = 0; i < m._matLists.Count; i++)
				{
					subMeshList = m._subMeshLists[i];
					string header = meshContainsName;
					GameObject mergedObject = new GameObject(header + "_" + m._objectLists[i][0].name + "_" + m._matLists[i][0].name, typeof(MeshFilter), typeof(MeshRenderer));
					Mesh combinedMesh = CombineSubmeshes(subMeshList, mergedObject);

					if (m._tranMeshColLists.Count != 0)
					{
						int mesColCou = 0;
						for(int t = 0 ; t< m._tranMeshColLists.Count; t++)
						{
							GameObject addMesh = new GameObject (m._meshColLists[t].name+"_"+mesColCou);
							mesColCou++;
							MeshCollider mc = addMesh.AddComponent<MeshCollider> ();
							mc.sharedMesh = m._meshColLists[t];
							addMesh.transform.position = m._tranMeshColLists[t].transform.position;
							addMesh.transform.rotation = m._tranMeshColLists[t].transform.rotation;
							addMesh.transform.localScale = m._tranMeshColLists[t].localScale;
							addMesh.transform.SetParent (mergedObject.transform);
						}

					}

					AssetDatabase.CreateAsset(combinedMesh, filePath + "/" + header + "_" + m._objectLists[i][0].name + "_" + m._matLists[i][0].name + ".asset");
					mergedObject.GetComponent<MeshFilter>().mesh = combinedMesh;
					mergedObject.GetComponent<MeshRenderer>().sharedMaterials = m._matLists[i].ToArray();
					lodList.Add (mergedObject);

					Renderer[] renderers = new Renderer[1];
					renderers[0] = mergedObject.GetComponent<Renderer>();
					lods[lodCount] = new LOD(1.0f / (lodCount+2f), renderers);
					lodCount++;

				}
			}
			string m_header = meshContainsName;
			GameObject newMergedObj = new GameObject (m_header+ "_" + key);
			foreach(GameObject n in lodList)
			{
				n.transform.SetParent (newMergedObj.transform);
			}

			if(lodGroup)
			{
				LODGroup lodObj =	newMergedObj.AddComponent<LODGroup> ();
				lodObj.SetLODs (lods);
				lodObj.RecalculateBounds ();
				for(int n = lodObj.GetLODs().Count()-1 ; n >=0 ; n--)
				{
					if(lodObj.GetLODs()[n].renderers.Length == 0)
					{
						List<LOD>  _lodObj  = lodObj.GetLODs ().ToList ();
						_lodObj.RemoveAt (n);
						lodObj.SetLODs (_lodObj.ToArray());
					}
				}
				if(lodRTH.ContainsKey(key))
				{
					lods = lodObj.GetLODs ();
					for(int u = 0 ; u < lodRTH[key].Count ; u ++)
					{
						lods [u].screenRelativeTransitionHeight = lodRTH [key] [u];
					}
					lodObj.SetLODs (lods);
				}
			}

			PrefabUtility.CreatePrefab( filePath + "/" + m_header + "_" + key+ ".prefab", (GameObject)newMergedObj, ReplacePrefabOptions.ConnectToPrefab);
			lodList.Clear ();
		}
	}

	void Patchchild (GameObject g)
	{
		sel.Clear ();
		List<GameObject> rootObj = new List<GameObject> ();
		rootObj.Add (g);
		while(rootObj.Count > 0)
		{
			GameObject root = rootObj [0];
			rootObj.RemoveAt (0);
			if(CheckMeshAndLOD(root))
			{
				sel.Add (root);
			}
			else
			{
				for(int c = 0 ; c < root.transform.childCount; c++)
				{
					rootObj.Add (root.transform.GetChild(c).gameObject);
				}
			}
		}
		polyCount = 0;
		while(sel.Count > 0)
		{
			polyCount = 0;
			int num = 0;
			int polygon = 0;
			int selCount = 0;
			Object mPrefab;
			GameObject m_sel = sel [0];
			sel.RemoveAt (0);
			mPrefab = PrefabUtility.GetPrefabParent (m_sel);

			sel = sel.OrderBy (go => Vector3.Distance(m_sel.transform.position,go.transform.position)).ToList();
			while(seObj.ContainsKey(mPrefab.name+"_Group_"+num))
			{	num ++;	}
			List<GameObject> newObj = new List<GameObject> ();
			newObj.Add (m_sel);
			seObj.Add (mPrefab.name+"_Group_"+num,newObj);
			selCount = sel.Count;
			while(selCount > 0)
			{
				selCount--;
				GameObject c_sel = sel [0];
				sel.RemoveAt (0);
				Object m_Prefab = PrefabUtility.GetPrefabParent (c_sel);
				if(mPrefab != m_Prefab)
				{
					sel.Add (c_sel);
					continue;
				}
				if (c_sel.GetComponent<MeshFilter> () != null)
				{
					polygon = c_sel.GetComponent<MeshFilter> ().sharedMesh.vertexCount;
					if (((polyCount + polygon)+polygon) > 65000)
					{
						sel.Add (c_sel);
						break;
					}

				}
				else if(c_sel.GetComponent<LODGroup>() != null)
				{
					polygon = c_sel.GetComponent<LODGroup> ().GetLODs()[0].renderers[0].GetComponent<MeshFilter> ().sharedMesh.vertexCount;
					if (((polyCount + polygon)+polygon) > 65000)
					{
						sel.Add (c_sel);
						break;
					}

				}
				polyCount += polygon;
				seObj [mPrefab.name+"_Group_"+num].Add (c_sel);
			}

		}
	}

	void Searching ()
	{
		sel.Clear ();
		if (draw.GetComponent<MeshFilter>() == null && draw.GetComponent<LODGroup>() == null)
		{
			canMerge = false;
			Debug.LogError ("This not a Object for Merge");
			return;
		}

		List<GameObject> rootObj = new List<GameObject> ();
		UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (rootObj);

		while(rootObj.Count > 0)
		{
			GameObject root = rootObj [0];
			rootObj.RemoveAt (0);
			Object drawPrefab = PrefabUtility.GetPrefabParent (draw);
			Object rootPrefab = PrefabUtility.GetPrefabParent (root);
			if(Vector3.Distance(draw.transform.position,root.transform.position) < radius
				&& CheckMeshAndLOD(root)
				&& drawPrefab.name == rootPrefab.name)
			{
				sel.Add (root);
			}
			else
			{
				for(int c = 0 ; c < root.transform.childCount; c++)
				{
					rootObj.Add (root.transform.GetChild(c).gameObject);
				}
			}
		}
		sel = sel.OrderBy (go => Vector3.Distance(draw.transform.position,go.transform.position)).ToList();
		polyCount = 0;
		foreach(GameObject m_sel in sel)
		{
			int polygon = 0;
			if (m_sel.GetComponent<MeshFilter> () != null)
			{
				if ((polyCount + m_sel.GetComponent<MeshFilter> ().sharedMesh.vertexCount) > 65000)
				{
					break;
				}
				polygon = m_sel.GetComponent<MeshFilter> ().sharedMesh.vertexCount;
			}
			else
			{
				if ((polyCount + m_sel.GetComponent<LODGroup> ().GetLODs()[0].renderers[0].GetComponent<MeshFilter> ().sharedMesh.vertexCount) > 65000)
				{
					break;
				}
				polygon = m_sel.GetComponent<LODGroup> ().GetLODs()[0].renderers[0].GetComponent<MeshFilter> ().sharedMesh.vertexCount;
			}
			sg.Add (m_sel);
			polyCount += polygon;
		}
	}

	void FullAuto ()
	{
		sel.Clear ();
		List<GameObject> rootObj = new List<GameObject> ();
		UnityEngine.SceneManagement.SceneManager.GetActiveScene ().GetRootGameObjects (rootObj);
		while(rootObj.Count > 0)
		{
			GameObject root = rootObj [0];
			rootObj.RemoveAt (0);
			if(CheckMeshAndLOD(root))
			{
				sel.Add (root);
				sg.Add (root);
			}
			else
			{
				for(int c = 0 ; c < root.transform.childCount; c++)
				{
					rootObj.Add (root.transform.GetChild(c).gameObject);
				}
			}
		}
		polyCount = 0;
		while(sel.Count > 0)
		{
			polyCount = 0;
			int num = 0;
			int polygon = 0;
			int selCount = 0;
			Object mPrefab;
			GameObject m_sel = sel [0];
			sel.RemoveAt (0);
			mPrefab = PrefabUtility.GetPrefabParent (m_sel);

			sel = sel.OrderBy (go => Vector3.Distance(m_sel.transform.position,go.transform.position)).ToList();
			while(seObj.ContainsKey(mPrefab.name+"_Group_"+num))
			{	num ++;	}
			List<GameObject> newObj = new List<GameObject> ();
			newObj.Add (m_sel);
			seObj.Add (mPrefab.name+"_Group_"+num,newObj);
			selCount = sel.Count;
			while(selCount > 0)
			{
				selCount--;
				GameObject c_sel = sel [0];
				sel.RemoveAt (0);
				Object m_Prefab = PrefabUtility.GetPrefabParent (c_sel);
				if(mPrefab != m_Prefab)
				{
					sel.Add (c_sel);
					continue;
				}
				if(Vector3.Distance(m_sel.transform.position,c_sel.transform.position) > radius)
				{
					sel.Add (c_sel);
					continue;
				}
				if (c_sel.GetComponent<MeshFilter> () != null)
				{
					polygon = c_sel.GetComponent<MeshFilter> ().sharedMesh.vertexCount;
					if (((polyCount + polygon)+polygon) > 65000)
					{
						sel.Add (c_sel);
						break;
					}

				}
				else if(c_sel.GetComponent<LODGroup>() != null)
				{
					polygon = c_sel.GetComponent<LODGroup> ().GetLODs()[0].renderers[0].GetComponent<MeshFilter> ().sharedMesh.vertexCount;
					if (((polyCount + polygon)+polygon) > 65000)
					{
						sel.Add (c_sel);
						break;
					}

				}
				polyCount += polygon;
				seObj [mPrefab.name+"_Group_"+num].Add (c_sel);
			}

		}
	}
	void OnDestroy()
	{
		if(draw != null)
		{
			DestroyImmediate (draw.GetComponent<DrawGiz>());
		}

	}

	void NewFolder (string header , string keyName)
	{

		scenePath = SceneManager.GetActiveScene().path;

		if (scenePath.Contains("/"))
			scenePath = scenePath.Substring(0, scenePath.LastIndexOf("/"));

		string folderPath = "Generated_" + SceneManager.GetActiveScene().name;

		if (!AssetDatabase.IsValidFolder(scenePath + "/" + folderPath))
			AssetDatabase.CreateFolder(scenePath, folderPath);
		scenePath += "/" + folderPath;
		if (!AssetDatabase.IsValidFolder(scenePath + "/" + "Merged"))
			AssetDatabase.CreateFolder(scenePath, "Merged");
		scenePath += "/Merged";


		int num = 0;
		while(AssetDatabase.IsValidFolder(scenePath+ "/" + header + "_" + keyName+"_"+num))
		{
			num++;
		}
		AssetDatabase.CreateFolder(scenePath,header + "_" + keyName+"_"+num);
		filePath = scenePath+ "/" + header + "_" + keyName+"_"+num;
		//								if (AssetDatabase.IsValidFolder(scenePath + "/" + header + "_" + key.name))
		//									AssetDatabase.CreateFolder(scenePath, header + "_" + key.name);
	}

	void ScenePath ()
	{
		//ScenePath
		scenePath = SceneManager.GetActiveScene().path;

		if (scenePath == null || scenePath.Length == 0) {
			return;
		}


		if (scenePath.Contains("/"))
			scenePath = scenePath.Substring(0, scenePath.LastIndexOf("/"));

		string folderPath = "Generated_" + SceneManager.GetActiveScene().name;

		if (!AssetDatabase.IsValidFolder(scenePath + "/" + folderPath))
			AssetDatabase.CreateFolder(scenePath, folderPath);
		scenePath += "/" + folderPath;
		if (!AssetDatabase.IsValidFolder(scenePath + "/" + "Merged"))
			AssetDatabase.CreateFolder(scenePath, "Merged");
		scenePath += "/Merged";
	}

	List<SubMesh> SubMeshToMesh (List<MeshFilter> mfs)
	{
		List<SubMesh> subMeshes = new List<SubMesh>();
		for (int i = 0; i < mfs[0].sharedMesh.subMeshCount; i++)
		{
			SubMesh sm = new SubMesh(new List<Transform>(), new List<Mesh>());
			subMeshes.Add(sm);
		}
		for (int a = 0; a < subMeshes.Count; a++)
		{
			for (int b = 0; b < mfs.Count; b++)
			{
				subMeshes[a].subTransforms.Add(mfs[b].transform);
				// TODo subMeshes[a].subMeshes.Add(MeshCreationHelper.CreateMesh(mfs[b].sharedMesh, a));
			}
		}
		return subMeshes;
	}

	List<Mesh> CombineMeshes (List<SubMesh> subMeshes)
	{
		List<Mesh> meshes = new List<Mesh>();
		for (int i = 0; i < subMeshes.Count; i++)
		{
			CombineInstance[] combine = new CombineInstance[subMeshes[i].subMeshes.Count];
			for (int j = 0; j < subMeshes[i].subMeshes.Count; j++)
			{
				combine[j].mesh = subMeshes[i].subMeshes[j];
				combine[j].transform = subMeshes[i].subTransforms[j].localToWorldMatrix;
			}
			Mesh combinedMesh = new Mesh();
			combinedMesh.CombineMeshes(combine);
			meshes.Add(combinedMesh);
		}
		return meshes;
	}

	Mesh CombineSubmeshes (List<SubMesh> subMeshes, GameObject obj)
	{
		CombineInstance[] combines;
		List<Mesh> meshes = new List<Mesh>();

		for (int i = 0; i < subMeshes.Count; i++)
		{
			combines = new CombineInstance[subMeshes[i].subMeshes.Count];

			for (int j = 0; j < subMeshes[i].subMeshes.Count; j++)
			{
				combines[j].mesh = subMeshes[i].subMeshes[j];
				combines[j].transform = subMeshes[i].subTransforms[j].localToWorldMatrix;
			}

			Mesh combinedMesh = new Mesh();
			combinedMesh.CombineMeshes(combines);
			meshes.Add(combinedMesh);
		}

		combines = new CombineInstance[meshes.Count];

		for (int i = 0; i < meshes.Count; i++)
		{
			combines[i].mesh = meshes[i];
			combines[i].transform = obj.transform.localToWorldMatrix;
		}

		Mesh allMesh = new Mesh();
		allMesh.CombineMeshes(combines, false);
		return allMesh;

	}

	void SaveNewMesh (List<Mesh> newMeshes)
	{
		if (!AssetDatabase.IsValidFolder(scenePath + "/" + meshContainsName))
			AssetDatabase.CreateFolder(scenePath, meshContainsName);
		scenePath += "/" + meshContainsName;
		for (int i = 0; i < newMeshes.Count; i++)
		{
			AssetDatabase.CreateAsset(newMeshes[i], scenePath + "/merge_" + meshContainsName + "_" + i + fileCount + ".asset");
			GameObject mergedObject = new GameObject("Merged_" + meshContainsName + "_" + i + fileCount, typeof(MeshFilter), typeof(MeshRenderer));
			mergedObject.GetComponent<MeshFilter>().mesh = newMeshes[i];
			mergedObject.GetComponent<MeshRenderer>().sharedMaterial = sharedMats[i];
			PrefabUtility.CreatePrefab(scenePath + "/merge_" + meshContainsName + "_" + i + fileCount + ".prefab", (GameObject)mergedObject, ReplacePrefabOptions.ConnectToPrefab);
			fileCount++;
		}
	}

	void ObjectsToString ()
	{
		foreach (GameObject s in selectedObjs)
		{
			objectsToString += s.name + "\n";
		}
	}

	bool CompareList<T> (List<T> a, List<T> b)
	{
		var firstNotSecond = a.Except(b).ToList();
		var secondNotFirst = b.Except(a).ToList();

		return !firstNotSecond.Any() && !secondNotFirst.Any();
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
		if(obj.GetComponent<LODGroup>() != null)
		{
			LODGroup gLOD = obj.GetComponent<LODGroup>();
			for(int g = 0 ; g < gLOD.GetLODs().Count() ; g++)
			{
				LOD checkLOD = gLOD.GetLODs () [g];
				if(checkLOD.renderers.Length == 0 || checkLOD.renderers[0] == null)
				{
					Debug.LogWarning (obj.name+" LOD "+g +" = null",obj);
					return false;
				}
			}
		}
		return true;
	}

	void UpdateProgress()
	{
		progress = (Mathf.InverseLerp (0, totalcount, counting))*100;
		EditorUtility.DisplayProgressBar("Progress... "+(int)progress+" %", "", Mathf.InverseLerp(0,totalcount,counting));
	}

}

public class DrawGiz : MonoBehaviour {
	public float radius;
	void OnDrawGizmos() {
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position,radius);
	}
}
