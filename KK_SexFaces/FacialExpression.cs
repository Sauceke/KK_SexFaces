﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;

namespace KK_SexFaces
{
    public class FacialExpression
    {
        public string EyebrowExpression { get; set; }
        public float EyebrowOpenMax { get; set; }
        public string EyeExpression { get; set; }
        public float EyesOpenMax { get; set; }
        public bool EyesBlinkFlag { get; set; }
        public int LookEyesPattern { get; set; }
        public Vector3? EyesTargetPos { get; set; }
        public Quaternion? EyesTargetRot { get; set; }
        public string MouthExpression { get; set; }
        public float MouthOpenMax { get; set; }

        public string Serialize()
        {
            using (StringWriter writer = new StringWriter())
            {
                new XmlSerializer(typeof(FacialExpression)).Serialize(writer, this);
                return writer.ToString();
            }
        }

        public static FacialExpression Deserialize(string blob)
        {
            if (blob == null)
            {
                return null;
            }
            using (StringReader reader = new StringReader(blob))
            {
                return (FacialExpression)new XmlSerializer(typeof(FacialExpression)).Deserialize(reader);
            }
        }

        public static FacialExpression Capture(ChaControl chaControl)
        {
            var expression = new FacialExpression()
            {
                EyebrowExpression = DictToString(GetExpression(chaControl.eyebrowCtrl)),
                EyebrowOpenMax = chaControl.GetEyebrowOpenMax(),
                EyeExpression = DictToString(GetExpression(chaControl.eyesCtrl)),
                EyesOpenMax = chaControl.GetEyesOpenMax(),
                EyesBlinkFlag = chaControl.GetEyesBlinkFlag(),
                LookEyesPattern = chaControl.GetLookEyesPtn(),
                MouthExpression = DictToString(GetExpression(chaControl.mouthCtrl)),
                MouthOpenMax = chaControl.GetMouthOpenMax()
            };
            if (IsLookingAtFixedPosition(chaControl))
            {
                expression.EyesTargetPos = chaControl.objEyesLookTarget.transform.localPosition;
                expression.EyesTargetRot = chaControl.objEyesLookTargetP.transform.localRotation;
            }
            return expression;
        }

        public void Apply(ChaControl chaControl)
        {
            chaControl.eyebrowCtrl.ChangeFace(StringToDict(EyebrowExpression), true);
            chaControl.ChangeEyebrowOpenMax(EyebrowOpenMax);
            chaControl.eyesCtrl.ChangeFace(StringToDict(EyeExpression), true);
            chaControl.ChangeEyesOpenMax(EyesOpenMax);
            chaControl.ChangeEyesBlinkFlag(EyesBlinkFlag);
            chaControl.ChangeLookEyesPtn(LookEyesPattern);
            if (EyesTargetPos != null)
            {
                chaControl.objEyesLookTargetP.transform.localRotation = EyesTargetRot.Value;
                GameObject eyesTarget = new GameObject();
                eyesTarget.transform.SetParent(chaControl.objEyesLookTargetP.transform);
                eyesTarget.transform.localPosition = EyesTargetPos.Value;
                chaControl.ChangeLookEyesTarget(0, eyesTarget.transform);
            }
            else
            {
                chaControl.eyeLookCtrl.target = Camera.current.transform;
            }
            chaControl.mouthCtrl.ChangeFace(StringToDict(MouthExpression), true);
            chaControl.ChangeMouthOpenMax(MouthOpenMax);
        }

        private static bool IsLookingAtFixedPosition(ChaControl chaControl)
        {
            Vector3 setTarget = chaControl.objEyesLookTarget.transform.localPosition;
            Vector3 actualTarget = chaControl.eyeLookCtrl.target.localPosition;
            return (setTarget - actualTarget).magnitude == 0;
        }

        private static Dictionary<int, float> GetExpression(FBSBase fbs)
        {
            return (Dictionary<int, float>)typeof(FBSBase)
                .GetField("dictNowFace", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(fbs);
        }

        private static string DictToString(Dictionary<int, float> dict)
        {
            return dict
                .Select(pair => pair.Key + ":" + pair.Value)
                .Aggregate("", (a, b) => a + ";" + b);
        }

        private static Dictionary<int, float> StringToDict(string s)
        {
            return s.Split(';').Skip(1)
                .ToDictionary(pair => int.Parse(pair.Split(':')[0]),
                pair => float.Parse(pair.Split(':')[1]));
        }
    }
}
