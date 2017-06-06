using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NodeEditorFramework;

namespace NodeEditorFramework.RealTime
{

    public class RuntimeUIEditor : MonoBehaviour
    {
        public NodeEditorUserCache canvasCache = new NodeEditorUserCache();
        public RectTransform content;

        public Dictionary<Node, RTNode> nodemap = new Dictionary<Node, RTNode>();

        private void OnEnable()
        {
            NodeEditor.checkInit(false);
            NodeEditorCallbacks.OnAddNode += AddNode;
            NodeEditorCallbacks.OnAddNodeKnob += AddNodeKnob;
            NodeEditorCallbacks.OnAddConnection += AddConnection;
        }


        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void LoadFile()
        {
            canvasCache.LoadXml(Application.dataPath + "/hello.xml");
        }

        void AddNode(Node node)
        {
            Debug.Log("hello" + node.name);
            RTNode nodePanel = NodeEditorUI.NodePanel(node,content);
            Debug.Log("node created");
            nodemap.Add(node, nodePanel);
            //nodePanel.transform.parent = content;
            //nodePanel.transform.localPosition = node.position;
        }

        void AddNodeKnob(NodeKnob nodeKnob)
        {
            Debug.Log(nodeKnob.name + " on " + nodeKnob.body.name);
            if (nodemap.ContainsKey(nodeKnob.body))
            {


                RTNode panel = nodemap[nodeKnob.body];
                if (nodeKnob.GetType().Equals(typeof(NodeInput)))
                {
                    Debug.Log("input");
                    NodeEditorUI.InputKnob((NodeInput)nodeKnob, panel.m_transform);
                }
                else
                {
                    Debug.Log("output");
                    NodeEditorUI.OutputKnob((NodeOutput)nodeKnob, panel.m_transform);
                }
            }
        }

        void AddConnection(NodeInput input)
        {
            Debug.Log(input.name + " connected to " + input.connection.name);
        }
    }

}