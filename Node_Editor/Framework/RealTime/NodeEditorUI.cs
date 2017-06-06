using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NodeEditorFramework.RealTime
{
    public static class NodeEditorUI
    {
        public static RTNode NodePanel(Node node,RectTransform parent)
        {
            //GameObject panel = GameObject.Instantiate(Resources.Load<GameObject>("Realtime/Prefabs/RT_node_panel"),(Vector3)node.position,Quaternion.identity,parent) as GameObject;
            //panel.GetComponent<RTNode>().title = node.name;
            //RectTransform transform = (RectTransform)panel.transform;
            //panel.GetComponent<RTNode>().node = node;
            //transform.sizeDelta = new Vector2(node.rect.width, node.rect.height);
            RTNode panel = GameObject.Instantiate(Resources.Load<RTNode>("Realtime/Prefabs/RT_node_panel"), (Vector3)node.position, Quaternion.identity, parent);
            panel.name = node.name;
            panel.title = node.name;
            panel.node = node;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(node.rect.width, node.rect.height);


            foreach(NodeKnob nodeKnob in node.nodeKnobs)
            {
                if (nodeKnob.GetType().Equals(typeof(NodeInput)))
                {
                    RTInputKnob rtknob = NodeEditorUI.InputKnob((NodeInput)nodeKnob, panel.GetComponent<RectTransform>());
                    panel.nodeKnobs.Add(rtknob);
                }
                else
                {
                    RTOutputKnob rtknob = NodeEditorUI.OutputKnob((NodeOutput)nodeKnob, panel.GetComponent<RectTransform>());
                    panel.nodeKnobs.Add(rtknob);
                }
                
            }

            node.CreateUI(panel);

            return panel;
        }

        public static GameObject Label(string text)
        {
            Text txt = GameObject.Instantiate(Resources.Load<Text>("Realtime/Prefabs/RT_label"));
            txt.text = text;
            return txt.gameObject;
        }

        public static GameObject InputField()
        {
            return GameObject.Instantiate(Resources.Load<GameObject>("Realtime/Prefabs/RT_input"));
        }

        public static GameObject HorizontalGroup()
        {
            return GameObject.Instantiate(Resources.Load<GameObject>("Realtime/Prefabs/RT_horizontal_group"));
        }

        public static GameObject VerticalGroup()
        {
            return GameObject.Instantiate(Resources.Load<GameObject>("Realtime/Prefabs/RT_vertical_group"));
        }

        public static RTInputKnob InputKnob(NodeInput nodeKnob, RectTransform parent)
        {
            GameObject knob = GameObject.Instantiate(Resources.Load<GameObject>("Realtime/Prefabs/RT_Knob_input"),parent);
            RTInputKnob inputKnob = knob.GetComponent<RTInputKnob>();
            inputKnob.knob = nodeKnob;

            inputKnob.GetComponent<RectTransform>().localPosition = PositionKnob(nodeKnob);

            return inputKnob;
        }

        public static RTOutputKnob OutputKnob(NodeOutput nodeKnob, RectTransform parent)
        {
            GameObject knob = GameObject.Instantiate(Resources.Load<GameObject>("Realtime/Prefabs/RT_Knob_output"),parent);
            RTOutputKnob outputKnob = knob.GetComponent<RTOutputKnob>();
            outputKnob.knob = nodeKnob;
            outputKnob.GetComponent<RectTransform>().localPosition = PositionKnob(nodeKnob);

            return outputKnob;
        }

        private static Vector3 PositionKnob(NodeKnob knob)
        {
            Vector3 position = new Vector3();
            switch (knob.side)
            {
                case NodeSide.Bottom:
                case NodeSide.Top:
                    position.y = knob.side== NodeSide.Top ? knob.body.rect.height/2 : -knob.body.rect.height / 2;
                    position.x = -knob.body.rect.width / 2;
                                    
                    break;
                case NodeSide.Left:
                case NodeSide.Right:
                    position.x = knob.side == NodeSide.Right ? knob.body.rect.width / 2 : -knob.body.rect.width / 2;
                    position.y = knob.body.rect.height / 2;

                    break;

            }
            return position;
        }

        public static GameObject Button(string name, string text, Action callback)
        {
            GameObject button = new GameObject(name, typeof(UnityEngine.UI.Button));
            
            button.GetComponent<Button>().onClick.AddListener(() => { callback(); });

            return button;
        }

        
    }
}
