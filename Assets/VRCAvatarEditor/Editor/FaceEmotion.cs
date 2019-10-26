﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class FaceEmotion
    {
        public static readonly string SENDDATAASSET_PATH = "Assets/SendData.asset";

        public class AnimParam
        {
            public string objPath;
            public string blendShapeName;
            public float value;
            public bool isSelect;

            public AnimParam(string path, string propertyName, float value)
            {
                objPath = path;
                this.blendShapeName = propertyName.Replace("blendShape.", "");
                this.value = value;
                isSelect = true;
            }
        }

        /// <summary>
        /// 指定したBlendShapeのアニメーションファイルを作成する
        /// </summary>
        public static AnimationClip CreateBlendShapeAnimationClip(string fileName, string saveFolderPath, ref List<SkinnedMesh> skinnedMeshList, ref List<string> exclusions, GameObject rootObj)
        {
            AnimationClip animClip = new AnimationClip();

            if (fileName == "") fileName = "face_emotion";

            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (!skinnedMesh.isOpenBlendShapes) continue;

                string path = GetHierarchyPath(rootObj, skinnedMesh.renderer.gameObject);

                foreach (var blendshape in skinnedMesh.blendshapes)
                {
                    float keyValue;
                    AnimationCurve curve = new AnimationCurve();
                    if (!blendshape.isExclusion && blendshape.isContains)
                    {
                        keyValue = skinnedMesh.renderer.GetBlendShapeWeight(blendshape.id);

                        curve.keys = null;

                        curve.AddKey(0, keyValue);
                        curve.AddKey(1 / 60.0f, keyValue);

                        animClip.SetCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + blendshape.name, curve);

                    }
                }
            }

            AssetDatabase.CreateAsset(animClip, AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + fileName + ".anim"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return animClip;
        }

        /// <summary>
        /// 特定のオブジェクトまでのパスを取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetHierarchyPath(GameObject rootObj, GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.parent == null || parent.gameObject == rootObj) return path;

                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// 指定オブジェクト以下でBlendShapeがついているSkinnedMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>BlendShapeがついているSkinnedMeshRendererのリスト</returns>
        public static List<SkinnedMesh> GetSkinnedMeshListOfBlendShape(GameObject parentObj)
        {
            var skinnedMeshList = new List<SkinnedMesh>();

            var skinnedMeshes = parentObj.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var skinnedMesh in skinnedMeshes)
                if (skinnedMesh.sharedMesh.blendShapeCount > 0)
                    skinnedMeshList.Add(new SkinnedMesh(skinnedMesh));

            return skinnedMeshList;
        }

        /// <summary>
        /// BlendShapeの値をすべてリセットする
        /// </summary>
        public static void ResetAllBlendShapeValues(ref List<SkinnedMesh> skinnedMeshList)
        {
            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (!skinnedMesh.isOpenBlendShapes) continue;
                
                foreach (var blendshape in skinnedMesh.blendshapes)
                    if (blendshape.isContains)
                        SetBlendShapeMinValue(ref skinnedMesh.renderer, blendshape.id);
            }
        }

        /// <summary>
        /// BlendShapeの値を最大値にする
        /// </summary>
        public static bool SetBlendShapeMaxValue(ref SkinnedMeshRenderer renderer, int id)
        {
            float maxValue = 0f;

            if (renderer.sharedMesh != null)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null) return false;
                
                for (int frameIndex = 0; frameIndex < mesh.GetBlendShapeFrameCount(id); frameIndex++)
                    maxValue = Mathf.Max(maxValue, mesh.GetBlendShapeFrameWeight(id, frameIndex));

                renderer.SetBlendShapeWeight(id, maxValue);

                return true;
            }

            return false;
        }

        /// <summary>
        /// BlendShapeの値を最小値にする
        public static bool SetBlendShapeMinValue(ref SkinnedMeshRenderer renderer, int id)
        {
            float minValue = 0f;

            if (renderer.sharedMesh != null)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null) return false;

                for (int frameIndex = 0; frameIndex < mesh.GetBlendShapeFrameCount(id); frameIndex++)
                    minValue = Mathf.Min(minValue, mesh.GetBlendShapeFrameWeight(id, frameIndex));

                renderer.SetBlendShapeWeight(id, minValue);

                return true;
            }
            return false;
        }

        /// <summary>
        /// すべてのBlendshapeのisContainの値を変更する
        /// </summary>
        /// <param name="value"></param>
        /// <param name="blendshapes"></param>
        public static bool SetContainsAll(bool value, ref List<SkinnedMesh.BlendShape> blendshapes)
        {
            if (blendshapes == null) return false;

            foreach (var blendshape in blendshapes)
                blendshape.isContains = value;

            return true;
        }
        
        /// <summary>
        /// アニメーションファイルを選択し, BlendShapeのデータを読み込む
        /// 読み込んだデータはSendData.assetに格納される
        /// </summary>
        /// <param name="sendData"></param>
        /// <param name="preWindow"></param>
        public static bool LoadAnimationProperties(ref SendData sendData, EditorWindow preWindow) {
            
                    string animFilePath = EditorUtility.OpenFilePanel("Select Loading Animation File", "Assets", "anim");
                    
                    if (animFilePath == "") return false;

                    animFilePath = FileUtil.GetProjectRelativePath(animFilePath);

                    sendData.filePath = animFilePath;
                    sendData.window = preWindow;            
                    // データ送受信用assetを作成する
                    AssetDatabase.CreateAsset (sendData, SENDDATAASSET_PATH);
                    AssetDatabase.Refresh ();

                    return true;
        }

        /// <summary>
        /// 選択したblendshapeのプロパティを反映する
        /// </summary>
        /// <param name="animProperties"></param>
        /// <param name="skinnedMeshes"></param>
        public static void ApplyAnimationProperties(List<AnimParam> animProperties, ref List<SkinnedMesh> skinnedMeshes) 
        {
            for (int skinnedMeshIndex = 0; skinnedMeshIndex < skinnedMeshes.Count; skinnedMeshIndex++) {
                var mesh = skinnedMeshes[skinnedMeshIndex].mesh;
                var renderer = skinnedMeshes[skinnedMeshIndex].renderer;

                foreach (var animProperty in animProperties) {
                    var index = mesh.GetBlendShapeIndex(animProperty.blendShapeName);
                    if (index >= 0) {
                        renderer.SetBlendShapeWeight(index, animProperty.value);
                    }
                }
            }

            // データの処理は終わったのでデータ送受信用assetを削除する
            AssetDatabase.DeleteAsset(SENDDATAASSET_PATH);
        }

        public static void ApplyAnimationProperties(AnimationClip clip, ref List<SkinnedMesh> skinnedMeshes)
        {
            var paramList = GetAnimationParamaters(clip);
            ApplyAnimationProperties(paramList, ref skinnedMeshes);
        }

        public static List<AnimParam> GetAnimationParamaters(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var animParamList = new List<AnimParam>();

            foreach (var binding in bindings)
            {
                if ((binding.propertyName).Split('.')[0] != "blendShape") continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var animParam = new AnimParam(binding.path, binding.propertyName, curve[0].value);
                animParamList.Add(animParam);
            }

            return animParamList;
        }
    }
}

