﻿using System.Collections.Generic;
using VRCAvatarEditor;
using UnityEditor;

namespace VRCAvatarEditor
{
    public class SendData : ScriptableSingleton<SendData>
    {
        public string filePath;
        public List<FaceEmotion.AnimParam> loadingProperties;
    }
}