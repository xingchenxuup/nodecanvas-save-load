using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using NodeCanvas.StateMachines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NodeCanvasLoadEditor : EditorWindow
{
    string blackboardKey = "";
    string dataPath;
    private Dictionary<string,string> BBDic = new Dictionary<string, string>();
    private bool foldoutType;     
#region 序列化数组

    [SerializeField]//必须要加
    protected List<GameObject> _assetLst = new List<GameObject>();
    //序列化对象
    protected SerializedObject _serializedObject;
    //序列化属性
    protected SerializedProperty _assetLstProperty;

    public string[] options = new string[]{"FSM","行为树","对话树"};
    public int index = 0;
    private BlackboardType blackboardType;

    public string blackboardAddKey;
    public Blackboard blackboardAdd;
#endregion
   

    Blackboard blackboard;
    private Graph graph;
    

    //利用构造函数来设置窗口名称
    NodeCanvasLoadEditor()
    {
        this.titleContent = new GUIContent("黑板数据保存与加载");
    }

    //添加菜单栏用于打开窗口
    [MenuItem("Tools/ParadoxNotion/NodeCanvas/黑板数据保存与加载")]
    static void showWindow()
    {
        GetWindow(typeof(NodeCanvasLoadEditor));
    }

    private void OnEnable()
    {
        dataPath = Application.dataPath + "/Resources/NodeCanvas/Blackboard";
        ReadFile();
        
#region 序列化数组
        //使用当前类初始化
        _serializedObject = new SerializedObject(this);
        //获取当前类中可序列话的属性
        _assetLstProperty = _serializedObject.FindProperty("_assetLst");
#endregion
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        //绘制标题
        GUILayout.Space(10);
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("NodeCanvas小插件");

        //绘制当前正在编辑的场景
        GUILayout.Space(10);
        GUI.skin.label.fontSize = 12;
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUILayout.Label("当前场景：" + EditorSceneManager.GetActiveScene().name);

        //绘制当前时间
        GUILayout.Space(10);
        GUILayout.Label("当前时间：" + DateTime.Now);

        EditorGUILayout.EditorToolbar();

        //绘制文本
        GUILayout.Space(10);
        blackboardKey = EditorGUILayout.TextField("Blackboard Key", blackboardKey);

        //绘制对象
        GUILayout.Space(10);
        blackboard = (Blackboard) EditorGUILayout.ObjectField("Blackboard", blackboard, typeof(Blackboard), true);

        // //绘制描述文本区域
        // GUILayout.Space(10);
        // GUILayout.BeginHorizontal();
        // GUILayout.Label("Description",GUILayout.MaxWidth(80));
        // description = EditorGUILayout.TextArea(description,GUILayout.MaxHeight(75));
        // GUILayout.EndHorizontal();

        EditorGUILayout.EditorToolbar();
        
        if (GUILayout.Button("保存Json文件"))
        {
            if ((blackboardKey!=null&& blackboardKey.Trim().Length>0)&&(blackboard!=null))
            {
                Save();
            }
        }

        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        EditorGUILayout.EditorToolbar();
        
        
        if (foldoutType = EditorGUILayout.Foldout(foldoutType, "Json数据"))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Key", EditorStyles.boldLabel);
            GUILayout.Label("Value", EditorStyles.boldLabel);
            GUILayout.Label("Del", EditorStyles.boldLabel, GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();
            GUIStyle textFieldStyle = new GUIStyle (GUI.skin.textField);
            textFieldStyle.focused.textColor = new Color(0.5f,0.5f,1);
            if (BBDic.Count == 0)
            {
                // EditorGUILayout.HelpBox("暂无数据", MessageType.Info,false); 
                EditorGUILayout.HelpBox("暂无数据", MessageType.Info); 
            }
            else
            {
                foreach (var bb in BBDic)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.TextField(bb.Key, textFieldStyle);
                    EditorGUILayout.TextField(bb.Value, textFieldStyle);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        DelJson(bb.Key);
                    }
                    EditorGUILayout.EndHorizontal();
                } 
            }
        }
        EditorGUILayout.Space();
        // EditorGUILayout.EditorToolbar();
        if (GUILayout.Button("刷新Json"))
        {
            ReadFile();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.EditorToolbar();

        AddGraphGUI();

    }

    
    private void AddGraphGUI()
    {
        blackboardType = (BlackboardType)GUILayout.SelectionGrid((int)blackboardType, new string[] { "Json文件加载", "黑板实例加载" }, 2);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GraphOwner Type",GUILayout.Width(150));
        index = EditorGUILayout.Popup(index,options,GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        
        switch (blackboardType)
        {
            case BlackboardType.Json:
                blackboardAddKey = EditorGUILayout.TextField("Blackboard Key", blackboardAddKey);
                break;
            case BlackboardType.Blackboard:
                blackboardAdd = (Blackboard) EditorGUILayout.ObjectField("Blackboard", blackboardAdd, typeof(Blackboard), true);
                break;
        }
        
        // blackboard = (Blackboard) EditorGUILayout.ObjectField("Blackboard", blackboard, typeof(Blackboard), true);
        // graphOwner = (GraphOwner) EditorGUILayout.ObjectField("GraphOwner", graphOwner, typeof(GraphOwner), true);
        if(index == 0)
        {
            graph = (Graph) EditorGUILayout.ObjectField("Graph", graph, typeof(FSM), true);
        }
        else if(index == 1)
        { 
            graph = (Graph) EditorGUILayout.ObjectField("Graph", graph, typeof(BehaviourTree), true);
        }
        else if(index == 2)
        { 
            graph = (Graph) EditorGUILayout.ObjectField("Graph", graph, typeof(DialogueTree), true);
        }
        
        #region 序列化数组
        //更新
        _serializedObject.Update();
        //开始检查是否有修改
        EditorGUI.BeginChangeCheck();
        //显示属性
        //第二个参数必须为true，否则无法显示子节点即List内容
        EditorGUILayout.PropertyField(_assetLstProperty, true);
        //结束检查是否有修改
        if (EditorGUI.EndChangeCheck())
        {         
            _serializedObject.ApplyModifiedProperties();
        }
        #endregion

        
        if (GUILayout.Button("批量添加脚本"))
        {
            InitGraph();
        }
    }





    //用于保存当前信息
    void Save()
    {
        string filePath = dataPath + "/" + blackboardKey + ".BB";
        if (File.Exists(filePath))
        {
            if (EditorUtility.DisplayDialog("文件重复", "已存在同名文件，是否覆盖", "覆盖", "取消"))
            {
                // Debug.Log("您点击了OK按钮");
                var serialize = blackboard.Serialize(null, true);
                Save(blackboardKey, serialize);
            }
            else
            {
                // Debug.Log("您点击了Cancel按钮");
            }
        }
        else
        {
            var serialize = blackboard.Serialize(null, true);
            Save(blackboardKey, serialize);
        }

    }


    private void InitGraph()
    {
        if (!AddCheck())
        {
            return;
        }
        
        foreach (var item in _assetLst)
        {
            if(index == 0)
            {
                var addComponent = item.AddComponent<FSMOwner>();
                var bb = item.GetComponent<Blackboard>();
                InitGraphBB(ref bb);
                addComponent.behaviour = (FSM) graph;
                // addComponent.StartBehaviour((FSM)graph);
            }
            else if(index == 1)
            {
                var addComponent = item.AddComponent<BehaviourTreeOwner>();
                var bb = item.GetComponent<Blackboard>();
                InitGraphBB(ref bb);
                addComponent.behaviour = (BehaviourTree) graph;
                // addComponent.StartBehaviour((BehaviourTree)graph);
            }
            else if(index == 2)
            {
                var addComponent = item.AddComponent<DialogueTreeController>();
                var bb = item.GetComponent<Blackboard>();
                InitGraphBB(ref bb);
                addComponent.behaviour = (DialogueTree) graph;
                // addComponent.StartBehaviour((DialogueTree)graph);
            }
            
        }
    }
    public void InitGraphBB(ref Blackboard bb)
    {
        switch (blackboardType)
        {
            case BlackboardType.Json:
                var load = Load(blackboardAddKey);
                if (load.Length == 0)
                {
                    EditorUtility.DisplayDialog("数据缺失", "未找到指定key的黑板数据", "确认");
                }
                bb.Deserialize(load, null, true);
                break;
            case BlackboardType.Blackboard:
                var json = blackboardAdd.Serialize(null);
                bb.Deserialize(json, null, true);
                break;
        }
    }
    public bool AddCheck()
    {
        if (((blackboardType == BlackboardType.Json && blackboardAddKey != null && blackboardAddKey.Trim().Length > 0)
            ||(blackboardType == BlackboardType.Blackboard && blackboardAdd != null))
            && graph != null)
        {
            return true;
        }
        EditorUtility.DisplayDialog("参数缺失", "参数不完整", "确认");
        return false;
    }
    

    

    /// <summary>
    /// 保存json
    /// </summary>
    /// <param name="key"></param>
    /// <param name="values"></param>
    private void Save(String key, String values)
    {
        // Debug.Log(values);
        StreamWriter sw = null;
        {
            try
            {
                sw = new StreamWriter(dataPath+"/"+key+".BB", false, Encoding.UTF8);
                sw.Write(values);
                sw.Flush();
            }
            catch
            {
                Debug.LogError("写入文件出错");
            }
        }
        sw.Close();
        sw.Dispose();
        ReadFile();
    }
    
    /// <summary>
    /// 保存json
    /// </summary>
    /// <param name="key"></param>
    private string Load(String key)
    {
        string filePath = dataPath + "/" + key + ".BB";
        if (File.Exists(filePath))
        {
            StreamReader reader = new StreamReader(filePath);
            return reader.ReadToEnd();
        }
        else
        {
            return "";
        }


    }

    
    /// <summary>
    /// 读取json文件
    /// </summary>
    private void ReadFile()
    {
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
            return;
        }
        BBDic = new Dictionary<string, string>();
        DirectoryInfo theFolder = new DirectoryInfo(dataPath);
        foreach(FileInfo nextFile in theFolder.GetFiles())
        {
            var fileName = nextFile.Name;
            if (fileName.EndsWith(".BB"))
            {
                StreamReader reader = new StreamReader(dataPath+"/"+fileName);
                string str = reader.ReadToEnd();
                str = str.Replace((char)13, (char)0).Replace((char)10, (char)0);
                fileName = fileName.Substring(0, fileName.Length - 3);
                BBDic.Add(fileName,str);
                reader.Close();
            }
        }
    }

    private void DelJson(string key)
    {
        if (EditorUtility.DisplayDialog("数据清理", "是否删除key为" + key + "的json数据", "确认", "取消"))
        {
            string filename = dataPath + "/" + key + ".BB";
            string meta = dataPath + "/" + key + ".BB.meta";
            File.Delete(filename);
            File.Delete(meta);
            ReadFile();
        }
    }


    public enum BlackboardType
    {
        Json,
        Blackboard,
    }
}

