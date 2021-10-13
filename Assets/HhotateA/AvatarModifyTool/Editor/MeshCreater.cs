/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using Random = UnityEngine.Random;
using System.Threading;
using UnityEngine.Rendering;

namespace HhotateA.AvatarModifyTools.Core
{
    public class MeshCreater
    {
        Mesh asset;
        public event Action<Mesh> OnReloadMesh;
        public string name;
        
        // 頂点の固有データ
        List<Vector3> vertexs = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        List<Color> colors = new List<Color>();
        List<Vector2>[] uvs = Enumerable.Range(0, 8).Select(_ => new List<Vector2>()).ToArray();
        List<BoneWeight> boneWeights = new List<BoneWeight>();
        // サブメッシュのデータ
        List<List<int>> triangles = new List<List<int>>();
        List<Material> materials = new List<Material>();
        List<Transform> meshTransforms = new List<Transform>(); //MeshRendererのTransform
        // ボーン情報
        List<Transform> bones = new List<Transform>();
        List<Matrix4x4> bindPoses = new List<Matrix4x4>(); // 一応事前計算してるけど，メッシュ化時でもいいかも
        // その他メッシュの固有情報
        List<BlendShapeData> blendShapes = new List<BlendShapeData>();

        // Undo,Redo用のキャッシュ
        // 頂点位置の履歴(index0は常に初期位置の記録(BlendShapeに基準となる))
        private List<List<Vector3>> vertexsCaches = new List<List<Vector3>>();
        // 最大キャッシュ数
        private int maxCaches
        {
            get => EnvironmentVariable.maxCaches;
        }
        // 現在参照中のキャッシュインデックス
        private int currentCacheIndex = -1;

        public bool IsRecalculateNormals { get; set; } = false;
        public bool IsRecalculateBlendShapeNormals { get; set; } = false;
        Transform rendBone; // 追加したRendererのTransform
        Transform rootBone; // 追加したskinmeshのrootBone
        public Transform RendBone
        {
            get { return rendBone; }
            set { rendBone = value; }
        }
        public Transform RootBone
        {
            get { return rootBone; }
            set { rootBone = value; }
        }

        public int VertexsCount()
        {
            return vertexs.Count;
        }

        public int TrianglesCount()
        {
            return triangles.Select(t => t.Count).Sum();
        }

        public MeshCreater(string n = "")
        {
            name = n;
        }
        
        public MeshCreater(Renderer rend,Transform avatarRoot = null)
        {
            if (rend.GetMesh() == null)
            {
                throw new NullReferenceException("Missing Mesh");
            }
            if (rend is SkinnedMeshRenderer)
            {
                // メッシュのセットアップ
                var mesh = rend as SkinnedMeshRenderer;
                var originMesh = mesh.sharedMesh;
                name = mesh.name;
                rendBone = rend.transform;
                rootBone = mesh.rootBone;
                AddSkinnedMesh(mesh);
                // skinmesh はbakeするので いらないかも？
                Create(originMesh);
            }
            else
            if (rend is MeshRenderer)
            {
                // メッシュのセットアップ
                var mesh = rend.GetComponent<MeshFilter>() as MeshFilter;
                var originMesh = mesh.sharedMesh;
                name = mesh.sharedMesh.name;
                rendBone = rend.transform;
                AddMesh(mesh.sharedMesh,rend.sharedMaterials,null,rend.transform);
                rootBone = rend.transform;
                Create(originMesh);
            }
        }
        
        public MeshCreater(Mesh mesh)
        {
            name = mesh.name;
            AddMesh(mesh);
        }
        
        public MeshCreater(MeshCreater mc)
        {
            rendBone = mc.rendBone;
            rootBone = mc.rootBone;
            var defaultVertexs = new List<Vector3>();
            var editVertexs = new List<Vector3>();
            
            AddBones(mc.bones.ToArray());

            var us = mc.GetUVList();
            for (int i = 0; i < mc.vertexs.Count; i++)
            {
                AddVertex( mc.vertexs[i] ,  mc.normals[i], mc.tangents[i], mc.colors[i], us[i], mc.boneWeights[i]);
            }

            for (int i = 0; i < mc.triangles.Count; i++)
            {
                AddSubMesh(mc.triangles[i],mc.materials[i],mc.meshTransforms[i]);
            }
            
            defaultVertexs.AddRange(mc.DefaultVertexs());
            editVertexs.AddRange(mc.EditVertexs());
            for (int i = 0; i < mc.blendShapes.Count; ++i)
            {
                blendShapes.Add(mc.blendShapes[i]);
            }

            AddCaches();
        }
        
        /// <summary>
        /// 複数のMeshCreaterを合体する
        /// </summary>
        /// <param name="mcs"></param>
        public MeshCreater(Transform avatarRoot,MeshCreater[] mcs)
        {
            rendBone = avatarRoot;
            var defaultVertexs = new List<Vector3>();
            var editVertexs = new List<Vector3>();

            foreach (var mc in mcs)
            {
                if (rootBone == null) rootBone = mc.rootBone;
                var offset = vertexs.Count;
                
                var boneTable = AddBones(mc.bones.ToArray());

                var us = mc.GetUVList();
                var mat = rendBone.worldToLocalMatrix * mc.rendBone.localToWorldMatrix;
                for (int i = 0; i < mc.vertexs.Count; i++)
                {
                    AddVertex(mat.MultiplyPoint(mc.vertexs[i]) ,
                    mat.MultiplyPoint(mc.normals[i]) ,
                        mc.tangents[i],mc.colors[i],us[i],mc.boneWeights[i],boneTable.ToArray());
                }

                for (int i = 0; i < mc.triangles.Count; i++)
                {
                    AddSubMesh(mc.triangles[i],mc.materials[i],mc.meshTransforms[i],offset);
                }
                
                defaultVertexs.AddRange(mc.DefaultVertexs());
                editVertexs.AddRange(mc.EditVertexs());
                for (int i = 0; i < mc.blendShapes.Count; ++i)
                {
                    //blendShapes.Add(mc.blendShapes[i].Clone().AddOffset(offset).TransformRoot(mc.RendBone,rendBone));
                    blendShapes.Add(mc.blendShapes[i].Clone().AddOffset(offset).TransformRoot(mat));
                }
            }
            AddCaches();
        }
        
        /// <summary>
        /// SkinMeshを読み込む(ボーンを読み込む)
        /// </summary>
        /// <param name="rend"></param>
        /// <param name="bake"></param>
        public void AddSkinnedMesh(SkinnedMeshRenderer rend,bool bake = true)
        {
            if (rootBone == null) rootBone = rend.rootBone;
            
            var boneTable = AddBones(rend.bones);

            var blendWeights = Enumerable.Range(0, rend.sharedMesh.blendShapeCount).Select(n => rend.GetBlendShapeWeight(n)).ToArray();

            if (bake)
            {
                Matrix4x4 mat = Matrix4x4.identity;
                if (rend.bones != null && rend.sharedMesh.bindposes != null)
                {
                    for (int i = 0; i < rend.bones.Length && i < rend.sharedMesh.bindposes.Length ; i++)
                    {
                        if (rend.bones[i] != null)
                        {
                            // bindposeのベースになっているオリジナルのtransformを特定
                            // blendShapeに対して逆変換するといい感じになる（った）
                            mat = rend.bones[i].worldToLocalMatrix.inverse * rend.sharedMesh.bindposes[i]; //transform.localToWorldMatrix
                            break;
                        }
                    }
                }
                // トランスフォームのリセット
                var t = rend.transform;
                t.localPosition = Vector3.zero;
                t.rotation = Quaternion.identity;
                t.localScale = Vector3.one;
                /*var s = new Vector3(t.localScale.x/t.lossyScale.x,t.localScale.y/t.lossyScale.y,t.localScale.z/t.lossyScale.z);
                t.localScale = new Vector3(s.x/mat.inverse.lossyScale.x,s.y/mat.inverse.lossyScale.y,s.z/mat.inverse.lossyScale.z);
                t.localRotation = mat.rotation;*/

                int bcBefore = blendShapes.Count;
                int vcBefore = vertexs.Count;
                AddMesh(rend.sharedMesh,rend.sharedMaterials,boneTable,null,blendWeights);
                int bcAfter = blendShapes.Count;
                int vcAfter = vertexs.Count;
                for (int i = bcBefore; i < bcAfter; i ++)
                {
                    blendShapes[i].TransformRoot(t.worldToLocalMatrix*mat);
                }
                
                Mesh b = Mesh.Instantiate(rend.sharedMesh);
                // rend.BakeMesh(b,true); //unity2020にしてほしい
                rend.BakeMesh(b);
                TransformMesh(b,vcBefore);
                TransformMatrixVector( t.worldToLocalMatrix, vcBefore,vcAfter-vcBefore);
                //TransforTransform( t, vcBefore,vcAfter-vcBefore);
            }
            else
            {
                AddMesh(rend.sharedMesh,rend.sharedMaterials,boneTable,null,blendWeights);
            }
        }
        
        /// <summary>
        /// メッシュを読み込む
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="bonetable">skinmeshの場合，</param>
        public void AddMesh(Mesh origin, Material[] mats = null, int[] boneTable = null,Transform subMeshBone = null,float[] blendWeights = null)
        {
            var offset = vertexs.Count;
            if (string.IsNullOrWhiteSpace(name)) name = origin.name;
            if (boneTable == null) boneTable = new int[0];
            if (boneTable.Length != origin.vertexCount) boneTable = Enumerable.Range(0, origin.vertexCount).Select(n => n).ToArray();
            var vs = origin.vertices.Length == origin.vertexCount ? origin.vertices : null;
            var ns = origin.normals.Length == origin.vertexCount ? origin.normals : origin.GetCalculateNormals().ToArray();
            var ts = origin.tangents.Length == origin.vertexCount ? origin.tangents : origin.GetCalculateTangents().ToArray();
            var cs = origin.colors.Length == origin.vertexCount ? origin.colors : null;
            var us = origin.uv.Length == origin.vertexCount ? origin.UVArray() : null;
            var ws = origin.boneWeights.Length == origin.vertexCount ? origin.boneWeights : null;
            if (subMeshBone != null)
            {
                boneTable = AddBones(new Transform[1]{subMeshBone});
                ws = Enumerable.Range(0, origin.vertexCount).Select(_ => new BoneWeight()
                {
                    boneIndex0 = 0,
                    weight0 = 1f
                }).ToArray();
            }
            
            for (int i = 0; i < origin.vertexCount; i++)
            {
                AddVertex(vs?[i],ns?[i],ts?[i],cs?[i],us?[i],ws?[i],boneTable);
            }

            for (int i=0; i<origin.subMeshCount; i++)
            {
                AddSubMesh(origin.GetTriangles(i).ToList(),mats?.Length > i ? mats[i] : new Material(Shader.Find("Unlit/Texture")),subMeshBone,offset);
            }

            if (blendWeights == null)
            {
                blendWeights = new float[0];
            }
            if (blendWeights.Length != origin.blendShapeCount)
            {
                blendWeights = new float[origin.blendShapeCount];
            }
            
            for (int i = 0; i < origin.blendShapeCount; ++i)
            {
                blendShapes.Add(new BlendShapeData(origin,i,offset,blendWeights[i]));
            }
        }

        /// <summary>
        /// ポリゴンを読み込む
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public void AddTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            triangles[triangles.Count-1].Add(vertexs.Count);
            triangles[triangles.Count-1].Add(vertexs.Count+1);
            triangles[triangles.Count-1].Add(vertexs.Count+2);
            var ps = new Vector3[]{p0,p1,p2};
            var ts = new Vector3[]{Vector3.Normalize(p1 - p2),Vector3.Normalize(p2-p0),Vector3.Normalize(p0-p1)};
            var normal = Vector3.Cross(ts[0],ts[1]);
            
            for (int i = 0; i < 3; i++)
            {
                AddVertex(ps[i],normal,ts[i]);
            }
        }
        
        public void AddTriangle(int v0, int v1, int v2)
        {
            if (v0 != v1 && v0 != v2)
            {
                if (v0 < vertexs.Count && v1 < vertexs.Count && v2 < vertexs.Count)
                {
                    int submesh = triangles.Count - 1;
                    var t = GetTriangleList(new List<int>() {v0, 01, 02});
                    if (t.Count > 0)
                    {
                        submesh = GetSubmeshID(t[0]);
                    }
                    triangles[submesh].Add(v0);
                    triangles[submesh].Add(v1);
                    triangles[submesh].Add(v2);
                }
            }
        }

        public void AddVertex(Vector3? vertex, Vector3? normal = null, Vector4? tangent = null, Color? color = null,Vector2[] uv = null, BoneWeight? boneWeight = null, int[] boneTable = null,int? addBlendShapeOrigin = null)
        {
            var v = vertex ?? Vector3.zero;
            var n = normal ?? Vector3.up;
            var t = tangent ?? Vector3.right;
            var c = color ?? Color.white;
            if(boneTable!=null)
                if (boneTable.Length == 0)
                    boneTable = null;
            var b = new BoneWeight()
            {
                boneIndex0 = boneWeight?.boneIndex0 ?? 0,
                boneIndex1 = boneWeight?.boneIndex1 ?? 0,
                boneIndex2 = boneWeight?.boneIndex2 ?? 0,
                boneIndex3 = boneWeight?.boneIndex3 ?? 0,
                weight0 = boneWeight?.weight0 ?? 0f,
                weight1 = boneWeight?.weight1 ?? 0f,
                weight2 = boneWeight?.weight2 ?? 0f,
                weight3 = boneWeight?.weight3 ?? 0f
            };
            if(boneTable != null)
            {
                if (boneTable.Length > boneWeight?.boneIndex0) b.boneIndex0 = boneTable[boneWeight?.boneIndex0 ?? 0];
                if (boneTable.Length > boneWeight?.boneIndex1) b.boneIndex1 = boneTable[boneWeight?.boneIndex1 ?? 0];
                if (boneTable.Length > boneWeight?.boneIndex2) b.boneIndex2 = boneTable[boneWeight?.boneIndex2 ?? 0];
                if (boneTable.Length > boneWeight?.boneIndex3) b.boneIndex3 = boneTable[boneWeight?.boneIndex3 ?? 0];
            }
            vertexs.Add(v);
            normals.Add(n);
            tangents.Add(t);
            colors.Add(c);
            for (int i = 0; i < uvs.Length; i++)
            {
                if (uv == null)
                {
                    uvs[i].Add(Vector2.zero);
                }
                else
                if(uv.Length<i)
                {
                    uvs[i].Add(Vector2.zero);
                }
                else
                {
                    uvs[i].Add(uv[i]);
                }
            }
            boneWeights.Add(b);
            if (addBlendShapeOrigin != null)
            {
                AddBlendshapeIndex(addBlendShapeOrigin??-1);
            }
        }

        public void RemoveVertex(int index)
        {
            vertexs.RemoveAt(index);
            normals.RemoveAt(index);
            tangents.RemoveAt(index);
            colors.RemoveAt(index);
            for (int j = 0; j < uvs.Length; j++)
            {
                uvs[j].RemoveAt(index);
            }
            boneWeights.RemoveAt(index);
            RemoveBlendshapeIndex(index);
        }

        public void AddSubMesh(List<int> tris,Material mat,Transform subMeshBone = null,int offset = 0)
        {
            if (tris.Count % 3 == 0)
            {
                triangles.Add(tris.Select(t=>t+offset).ToList());
                materials.Add(mat);
                meshTransforms.Add(subMeshBone);
            }
        }

        /// <summary>
        // ボーンリストにボーン追加，変換表を作成
        /// </summary>
        /// <param name="rendBones"></param>
        /// <returns></returns>
        public int[] AddBones(Transform[] rendBones,bool allowNull = true)
        {
            List<int> boneTable = new List<int>();
            foreach(var bone in rendBones)
            {
                if (bone)
                {
                    if (!bones.Contains(bone))
                    {
                        var bindPose = bone.worldToLocalMatrix * rendBone.localToWorldMatrix;
                        bindPoses.Add(bindPose);
                        bones.Add(bone);
                    }
                    boneTable.Add(bones.FindIndex(b=>b == bone));
                }
                else
                {
                    // ボーンが存在しない場合
                    boneTable.Add(-1);
                    if (allowNull)
                    {
                        // 不本意ながらnullBone入りメッシュでバグるので(当然)
                        bindPoses.Add(new Matrix4x4());
                        bones.Add(bone);
                    }
                }
            }

            return boneTable.ToArray();
        }

        public List<Transform> ApplyBoneTable(Dictionary<Transform, Transform> boneTable)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (boneTable.ContainsKey(bones[i]))
                {
                    bones[i] = boneTable[bones[i]];
                    bindPoses[i] = bones[i].worldToLocalMatrix * rendBone.localToWorldMatrix;
                }
            }

            return bones;
        }
        
        /// <summary>
        /// TriangleをIndexで削除する
        /// </summary>
        /// <param name="index"></param>
        public void RemoveTriangle(int index)
        {
            int i = 0;
            for (int j = 0; j < triangles.Count; j++)
            {
                for (int k = 0; k < triangles[j].Count; k+=3)
                {
                    if (i == index)
                    {
                        triangles[j].RemoveAt(k);
                        triangles[j].RemoveAt(k);
                        triangles[j].RemoveAt(k);
                    }
                    i++;
                }
            }
            neighborhoodList = new List<int>[0];
        }

        /// <summary>
        /// Triangleの連番IDからsubmeshのIDを取得する
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetSubmeshID(int index)
        {
            int i = 0;
            for (int j = 0; j < triangles.Count; j++)
            {
                for (int k = 0; k < triangles[j].Count; k+=3)
                {
                    if (i == index)
                    {
                        return j;
                    }
                    i++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Triangleを追加する
        /// </summary>
        /// <param name="submesh"></param>
        /// <param name="vs"></param>
        public void AddTriangle(int submesh, List<int> vs)
        {
            if (vs.Count == 3)
            {
                triangles[submesh].AddRange(vs);
            }
            neighborhoodList = new List<int>[0];
        }

        public void AddBlendshapeIndex(int index = -1)
        {
            foreach (var blendShape in blendShapes)
            {
                blendShape.AddVertexIndex(vertexs.Count - 1, Vector3.zero, Vector3.zero, Vector3.zero);
            }
        }
        public void AddBlendshapeIndex(int[] origin,float[] weight = null)
        {
            foreach (var blendShape in blendShapes)
            {
                blendShape.AddVertexIndex(vertexs.Count - 1, origin, weight);
            }
        }

        public void RemoveBlendshapeIndex(int i)
        {
            foreach (var blendShape in blendShapes)
            {
                blendShape.RemoveVertexIndex(i);
            }
        }

        public int GetTriangleOffset(int submesh)
        {
            int offset = 0;
            for (int i = 0; i < submesh; i++)
            {
                offset += triangles[i].Count / 3;
            }

            return offset;
        }
        
        /// <summary>
        /// 頂点をすべて含むTriangleをリストで返す
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="subMesh"></param>
        /// <returns></returns>
        public List<int> GetTriangleList(List<int> verts,Action<int,List<int>> subMesh = null,int matchVertexes = 3)
        {
            var tris = new List<int>();
            int i = 0;
            for (int j = 0; j < triangles.Count; j++)
            {
                var tri = new List<int>();
                for (int k = 0; k < triangles[j].Count; k += 3)
                {
                    int match = 0;
                    if (verts.Contains(triangles[j][k])) match++;
                    if (verts.Contains(triangles[j][k + 1])) match++;
                    if (verts.Contains(triangles[j][k + 2])) match++;
                    if (match >= matchVertexes)
                    {
                        tris.Add(i);
                        tri.Add(triangles[j][k]);
                        tri.Add(triangles[j][k+1]);
                        tri.Add(triangles[j][k+2]);
                    }
                    i++;
                }
                subMesh?.Invoke(j,tri);
            }

            return tris;
        }
        
        /// <summary>
        /// Get triangleIndex by vertex Index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<int> GetTriangleIndex(int index)
        {
            var output = new List<int>();
            int i = 0;
            for (int j = 0; j < triangles.Count; j++)
            {
                for (int k = 0; k < triangles[j].Count; k+=3)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        if (triangles[j][k + l] == index)
                        {
                            output.Add(i);
                        }
                    }
                    i++;
                }
            }
            return output;
        }
        
        /// <summary>
        /// GetVertex by triangleIndex
        /// or null
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<int> GetVertexIndex(int index)
        {
            index *= 3;
            int i = 0;
            for (int j = 0; j < triangles.Count; j++)
            {
                if (i + triangles[j].Count < index)
                {
                    i += triangles[j].Count;
                }
                else
                {
                    var output = new List<int>();
                    var k = index - i;
                    for (int l = 0; l < 3; l++)
                    {
                        output.Add(triangles[j][k + l]);
                    }
                    return output;
                }
            }

            return null;
        }

        public Vector2[][] GetUVList()
        {
            return Enumerable.Range(0, vertexs.Count).Select(i => new Vector2[8]
            {
                uvs[0][i],uvs[1][i],uvs[2][i],uvs[3][i],uvs[4][i],uvs[5][i],uvs[6][i],uvs[7][i]
            }).ToArray();
        }

        public Material[] GetMaterials()
        {
            return materials.ToArray();
        }

        private List<int>[] neighborhoodList = new List<int>[0];
        
        /// <summary>
        /// 頂点に隣接している頂点の事前計算リスト
        /// </summary>
        private List<int>[] GetNeighborhoodList(bool? overlapping_null = null)
        {
            var overlapping = overlapping_null ??  isComputeLandVertexesOverlapping;
            if (neighborhoodList.Length != vertexs.Count || isComputeLandVertexesOverlapping != overlapping)
            {
                neighborhoodList = Enumerable.Range(0, vertexs.Count).Select(_=>new List<int>()).ToArray();
                for (int j = 0; j < triangles.Count; j++)
                {
                    for (int k = 0; k < triangles[j].Count; k+=3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            for (int ii = 0; ii < 3; ii++)
                            {
                                if(i!=ii) neighborhoodList[triangles[j][k+i]].Add(triangles[j][k+ii]);
                            }

                            neighborhoodList[triangles[j][k + i]] = neighborhoodList[triangles[j][k + i]].Distinct().ToList();
                        }
                    }
                }

                if (overlapping)
                {
                    for (int i = 0; i < vertexs.Count; i++)
                    {
                        neighborhoodList[i].AddRange(GetOverlap(i));
                    }
                }
                isComputeLandVertexesOverlapping = overlapping;

                for (int i = 0; i < vertexs.Count; i++)
                {
                    neighborhoodList[i] = neighborhoodList[i].Distinct().ToList();
                }
            }
            return neighborhoodList;
        }
        
        private List<int>[] overlapList = new List<int>[0];

        private List<int>[] GetOverlapList()
        {

            if (overlapList.Length != vertexs.Count)
            {
                overlapList = new List<int>[vertexs.Count];
                for (int i = 0; i < vertexs.Count; i++)
                {
                    var list = new List<int>();
                    for (int j = 0; j < vertexs.Count; j++)
                    {
                        if (i != j)
                        {
                            if (Vector3.Distance(vertexs[i], vertexs[j]) < 0.001f)
                            {
                                list.Add(j);
                            }
                        }
                    }
                    overlapList[i] = list;
                }
            }

            return overlapList;
        }
        
        /// <summary>
        /// 重複頂点の事前計算リスト
        /// </summary>
        private List<int> GetOverlap(int i)
        {
            return GetOverlapList()[i];
        }

        public bool IsComputeLandVertexes()
        {
            return token != null;
        }

        private CancellationTokenSource token;
        private bool isComputeLandVertexesOverlapping = true;

        /// <summary>
        /// 頂点リストの事前計算用(未使用)
        /// </summary>
        /// <param name="overlapping"></param>
        /// <returns></returns>
        public async Task ComputeNeighborhoodList(bool? overlapping = null)
        {
            token = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                GetNeighborhoodList(overlapping??isComputeLandVertexesOverlapping);
            },token.Token);
            await task;
            token = null;
        }
        
        /// <summary>
        /// 頂点に離接している頂点を再帰的に求める
        /// </summary>
        /// <param name="vert"></param>
        /// <param name="onFind"></param>
        /// <param name="onFinish"></param>
        /// <param name="overlapping"></param>
        /// <returns></returns>
        public async void ComputeLandVertexes(int vert, Action<int> onFind = null, Action<List<int>> onFinish = null, bool? overlapping = null)
        {
            var frontier = new List<int>() {vert};
            var output = new List<int>();
            if(IsComputeLandVertexes()) token.Cancel();
            token = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                foreach (var v in frontier)
                {
                    if (!output.Contains(v))
                    {
                        output.Add(v);
                        onFind?.Invoke(v);
                    }
                }

                while (frontier.Count != 0)
                {
                    foreach (var v in GetNeighborhoodList(overlapping??isComputeLandVertexesOverlapping)[frontier[0]])
                    {
                        if (!output.Contains(v))
                        {
                            output.Add(v);
                            frontier.Add(v);
                            onFind?.Invoke(v);
                        }
                    }

                    frontier.RemoveAt(0);
                }
            },token.Token);
            await task;
            token.Dispose();
            token = null;
            onFinish?.Invoke(output);
        }

        /// <summary>
        /// 頂点除くするTriangleをコピーする
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="subMesh"></param>
        public void CopyVertexes(List<int> verts,bool subMesh = true)
        {
            verts = verts.Distinct().ToList();
            Dictionary<int, int> offset = new Dictionary<int, int>();
            int offsetint = vertexs.Count;
            var us = GetUVList();
            for (int i = 0; i < verts.Count; i++)
            {
                var index = verts[i];
                offset.Add(index,vertexs.Count);
                AddVertex(vertexs[index],normals[index],tangents[index],colors[index],us[index],boneWeights[index]);
            }

            var wp = ComputeCenterPoint(verts);
            TransformAtDefault(Enumerable.Range(offsetint,vertexs.Count-offsetint).Select(_=>wp).ToList(),offsetint);
            
            GetTriangleList(verts, (j, vs) =>
            {
                if (vs.Count != 0 && vs.Count%3==0)
                {
                    if (subMesh)
                    {
                        AddSubMesh(vs.Select(v => offset[v]).ToList(),materials[j],meshTransforms[j]);
                    }
                    else
                    {
                        triangles[j].AddRange(vs.Select(v=>offset[v]).ToList());
                    }
                }
            });
        }

        /// <summary>
        /// 頂点の属するTriangleを削除する
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="removeAsVertexMove"></param>
        public void RemoveVertexesTriangles(List<int> verts,bool removeAsVertexMove = false,bool vertexRemove = false)
        {
            if (removeAsVertexMove)
            {
                var wp = ComputeCenterPoint(verts);
                TransformScale(verts, Vector3.zero,wp);
            }
            else
            {
                var tris = GetTriangleList(verts,null,vertexRemove?1:3);
                tris = tris.OrderBy(t => t).ToList();
                for (int i = 0; i < tris.Count; i++)
                {
                    RemoveTriangle(tris[i]-i);
                }

                if (vertexRemove)
                {
                    verts = verts.OrderBy(v => v).ToList();
                    var table = Enumerable.Range(0, vertexs.Count).ToList();
                    int offset = 0;
                    for (int i = 0; i < verts.Count; i++)
                    {
                        table.RemoveAt(verts[i] + offset);
                        RemoveVertex(verts[i]+offset);
                        offset--;
                    }

                    /*for (int i = 0; i < blendShapes.Count; i++)
                    {
                        blendShapes[i].ApplyTable(verts);
                    }*/

                    for (int i = 0; i < triangles.Count; i++)
                    {
                        for (int j = 0; j < triangles[i].Count; j++)
                        {
                            triangles[i][j] = table.FindIndex(l=>l==triangles[i][j]);
                        }
                    }
                }
            }
        }

        public void TrianglesTransform(List<int> triangle,Transform root,Transform transform,bool inverse = false)
        {
            if (root != null && transform != null)
            {
                var tris = triangle.Distinct().ToList();
                if (inverse)
                {
                    var mat = transform.worldToLocalMatrix * root.localToWorldMatrix;
                    TransformMatrix(tris,mat);
                }
                else
                {
                    var mat = root.worldToLocalMatrix * transform.localToWorldMatrix;
                    TransformMatrix(tris,mat);
                }
            }
        }
        public void TransformMatrix(List<int> verts, Matrix4x4 mat)
        {
            foreach (var v in verts)
            {
                vertexs[v] = mat.MultiplyPoint(vertexs[v]);
            }
        }
        
        public void TransformMatrixVector(Matrix4x4 mat, int from = 0, int length = -1)
        {
            if (length < 0) length = vertexs.Count - from; 
            for (int i = from; i < length + from; i ++)
            {
                vertexs[i] = mat.MultiplyVector(vertexs[i]);
            }
        }
        
        /// <summary>
        /// 頂点の属するTriangleにTransformを適応する
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        public void TransformVertexes(
            List<int> verts, 
            Vector3 position, 
            Vector3 rotation,
            Vector3 scale)
        {
            var wp = ComputeCenterPoint(verts);
            TransformScale(verts, scale, wp);
            TransformRotate(verts, rotation, wp);
            TransformPosition(verts, position - wp);
        }

        /// <summary>
        /// 頂点リストの重心を計算する
        /// </summary>
        /// <param name="verts"></param>
        /// <returns></returns>
        public Vector3 ComputeWeightPoint(List<int> verts)
        {
            var wp = Vector3.zero;
            foreach (var v in verts)
            {
                wp += vertexs[v];
            }
            return new Vector3(
                wp.x/verts.Count,
                wp.y/verts.Count,
                wp.z/verts.Count);
        }
        /// <summary>
        /// 頂点リストの中心を計算する
        /// </summary>
        /// <param name="verts"></param>
        /// <returns></returns>
        public Vector3 ComputeCenterPoint(List<int> verts)
        {
            if(verts.Count==0) return Vector3.zero;
            float xmax = vertexs[verts[0]].x;
            float ymax = vertexs[verts[0]].y;
            float zmax = vertexs[verts[0]].z;
            float xmin = vertexs[verts[0]].x;
            float ymin = vertexs[verts[0]].y;
            float zmin = vertexs[verts[0]].z;
            foreach (var v in verts)
            {
                xmax = Mathf.Max(xmax, vertexs[v].x);
                ymax = Mathf.Max(ymax, vertexs[v].y);
                zmax = Mathf.Max(zmax, vertexs[v].z);
                xmin = Mathf.Min(xmin, vertexs[v].x);
                ymin = Mathf.Min(ymin, vertexs[v].y);
                zmin = Mathf.Min(zmin, vertexs[v].z);
            }
            return new Vector3(
                (xmax+xmin)*0.5f,
                (ymax+ymin)*0.5f,
                (zmax+zmin)*0.5f);
        }
        
        /// <summary>
        /// 頂点を移動させる
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="position"></param>
        public void TransformPosition(List<int> verts, Vector3 position)
        {
            foreach (var v in verts)
            {
                vertexs[v] = vertexs[v] + position;
            }
        }
        /// <summary>
        /// 頂点を回転させる
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="rotate"></param>
        /// <param name="wp"></param>
        public void TransformRotate(List<int> verts, Vector3 rotate, Vector3 wp)
        {
            var rot = Quaternion.Euler(rotate);
            foreach (var v in verts)
            {
                vertexs[v] = vertexs[v] - wp;
                vertexs[v] = rot * vertexs[v];
                vertexs[v] = vertexs[v] + wp;
            }
        }
        /// <summary>
        /// 頂点を回転させる
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="rotate"></param>
        /// <param name="wp"></param>
        public void TransformRotate(List<int> verts, Quaternion rotate, Vector3 wp)
        {
            foreach (var v in verts)
            {
                vertexs[v] = vertexs[v] - wp;
                vertexs[v] = rotate * vertexs[v];
                vertexs[v] = vertexs[v] + wp;
            }
        }
        /// <summary>
        /// 頂点にスケールを適応する
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="scale"></param>
        /// <param name="wp"></param>
        public void TransformScale(List<int> verts, Vector3 scale, Vector3 wp)
        {
            foreach (var v in verts)
            {
                vertexs[v] = vertexs[v] - wp;
                vertexs[v] = new Vector3(
                    vertexs[v].x*scale.x,
                    vertexs[v].y*scale.y,
                    vertexs[v].z*scale.z);
                vertexs[v] = vertexs[v] + wp;
            }
        }

        public void WeightCopy(List<int> verts, int originIndex)
        {
            BoneWeight bw = boneWeights[originIndex];
            foreach (var v in verts)
            {
                boneWeights[v] = new BoneWeight()
                {
                    boneIndex0 = bw.boneIndex0,
                    boneIndex1 = bw.boneIndex1,
                    boneIndex2 = bw.boneIndex2,
                    boneIndex3 = bw.boneIndex3,
                    weight0 = bw.weight0,
                    weight1 = bw.weight1,
                    weight2 = bw.weight2,
                    weight3 = bw.weight3
                };
            }
        }
        
        /// <summary>
        /// 空間をBoxに分け，再帰的にBox内にポリゴンが存在するか判定する
        /// </summary>
        /// <param name="center"></param>
        /// <param name="extentV"></param>
        /// <param name="targetV"></param>
        /// <param name="aabbtriangles"></param>
        /// <param name="onHit"></param>
        public void AABBRecursiveTriangles(Vector3 center, float extentV, float targetV,List<int> aabbtriangles,Action<Vector3> onHit)
        {
            var newTriangles = AABBTriangles(center, new Vector3(extentV, extentV, extentV), aabbtriangles);
            if(newTriangles.Count > 0)
            {
                if (extentV <= targetV)
                {
                    onHit?.Invoke(center);
                    return;
                }
                extentV = extentV * 0.5f;
                var d = extentV * 0.5f;
                AABBRecursiveTriangles(center+new Vector3(d,d,d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(d,d,-d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(d,-d,d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(d,-d,-d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(-d,d,d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(-d,d,-d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(-d,-d,d),extentV,targetV,newTriangles,onHit);
                AABBRecursiveTriangles(center+new Vector3(-d,-d,-d),extentV,targetV,newTriangles,onHit);
            }
        }
        
        /// <summary>
        /// Box内にポリゴンが存在するか判定する
        /// </summary>
        /// <param name="center"></param>
        /// <param name="extent"></param>
        /// <param name="aabbtriangles"></param>
        /// <returns></returns>
        public List<int> AABBTriangles(Vector3 center, Vector3 extent,List<int> aabbtriangles)
        {
            var newTriangles = new List<int>();
            for (int i=0; i+2 < aabbtriangles.Count(); i+=3)
            {
                var v0 = vertexs[aabbtriangles[i]];
                var v1 = vertexs[aabbtriangles[i+1]];
                var v2 = vertexs[aabbtriangles[i+2]];
                float xmax = Mathf.Max(Mathf.Max(v0.x, v1.x), v2.x);
                float ymax = Mathf.Max(Mathf.Max(v0.y, v1.y), v2.y);
                float zmax = Mathf.Max(Mathf.Max(v0.z, v1.z), v2.z);
                float xmin = Mathf.Min(Mathf.Min(v0.x, v1.x), v2.x);
                float ymin = Mathf.Min(Mathf.Min(v0.y, v1.y), v2.y);
                float zmin = Mathf.Min(Mathf.Min(v0.z, v1.z), v2.z);

                if (center.x + extent.x > xmin && center.x - extent.x < xmax &&
                    center.y + extent.y > ymin && center.y - extent.y < ymax &&
                    center.z + extent.z > zmin && center.z - extent.z < zmax)
                {
                    newTriangles.Add(aabbtriangles[i]);
                    newTriangles.Add(aabbtriangles[i+1]);
                    newTriangles.Add(aabbtriangles[i+2]);
                }
            }

            return newTriangles;
        }

        /// <summary>
        /// ワールドベクトルをローカルベクトルに変換する
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public Vector3 CalculateLocalVec(Vector3 vec)
        {
            if(rendBone==null) return Vector3.zero;
            return rendBone.InverseTransformDirection(vec);
        }
        
        /// <summary>
        /// ワールド座標をローカル座標に変換する
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 CalculateLocalPos(Vector3 pos)
        {
            if(rendBone==null) return Vector3.zero;
            return rendBone.InverseTransformPoint(pos);
        }
        
        /// <summary>
        /// ローカルベクトルをワールドベクトルに変換する
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public Vector3 CalculateWorldlVec(Vector3 vec)
        {
            if(rendBone==null) return Vector3.zero;
            return rendBone.TransformDirection(vec);
        }

        /// <summary>
        /// ローカル座標をワールド座標に変換する
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 CalculateWorldPos(Vector3 pos)
        {
            if(rendBone==null) return Vector3.zero;
            return rendBone.TransformPoint(pos);
        }
        
        /// <summary>
        /// 基準点に近い頂点をvec方向に移動させる
        /// </summary>
        /// <param name="from">基準点</param>
        /// <param name="vec">方向</param>
        /// <param name="power"></param>
        /// <param name="width"></param>
        /// <param name="strength"></param>
        public void TransformMesh(Vector3 from, Vector3 vec,float power,float width,float strength)
        {
            for(int i = 0; i < vertexs.Count; i++)
            {
                if (vertexs[i] == from)
                {
                    vertexs[i] += vec*power;
                    continue;
                }
                var dist = Vector3.Distance(from, vertexs[i]);
                var multi = Mathf.Lerp(power, 0f, Mathf.Min(1f, Mathf.Pow(dist/width,strength)));
                vertexs[i] += vec * multi;
            }
        }
        
        /// <summary>
        /// 基準点に近い頂点をtoに移動させる
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="width"></param>
        /// <param name="strength"></param>
        public void TransformMesh(Vector3 from, Vector3 to,float width,float strength)
        {
            for(int i = 0; i < vertexs.Count; i++)
            {
                if (vertexs[i] == from)
                {
                    vertexs[i] = to;
                    continue;
                }
                var dist = Vector3.Distance(from, vertexs[i]);
                var delta = to - from;
                var multi = Mathf.Lerp(1f, 0f, Mathf.Min(1f, Mathf.Pow(dist/width,strength)));
                vertexs[i] += delta * multi;
            }
        }
        
        /// <summary>
        /// 頂点の位置リストにより変形する
        /// </summary>
        /// <param name="verts"></param>
        public void TransformMesh(Vector3[] verts)
        {
            // if (vertexs.Count == verts.Length)
            {
                var v = new List<Vector3>();
                
                foreach (var vert in verts)
                {
                    v.Add(vert);
                }

                vertexs = v;
            }
        }
        
        /// <summary>
        /// メッシュの頂点に変形させる
        /// </summary>
        /// <param name="mesh"></param>
        public void TransformMesh(Mesh mesh,int offset = 0)
        {
            // ここで取得しとく方が，いちいちmeshアクセスするより圧倒的に早い
            var vs = mesh.vertices;
            var ns = mesh.normals;
            var ts = mesh.tangents;
            if (mesh.vertices.Length + offset <= vertexs.Count)
            {
                for (int i = 0; i < vs.Length; i++)
                {
                    vertexs[i + offset] = vs[i];
                }
                /*if (root == null)
                {
                    for (int i = 0; i < vs.Length; i++)
                    {
                        vertexs[i + offset] = vs[i];
                    }
                }
                else
                {
                    for (int i = 0; i < vs.Length; i++)
                    {
                        // 秘伝のタレ的コード
                        // ベイクにTransformが適応されるので逆変換
                        // 方向を治してからscaleを補正している（たぶん）
                        vertexs[i + offset] = root.InverseTransformVector(root.TransformDirection(vs[i]));
                    }
                }*/
            }

            if (ns.Length == mesh.vertexCount)
            {
                for (int i = 0; i < vs.Length; i++)
                {
                    normals[i + offset] = ns[i];
                }
            }
            if (ts.Length == mesh.vertexCount)
            {
                for (int i = 0; i < vs.Length; i++)
                {
                    tangents[i + offset] = ts[i];
                }
            }
        }

        public Transform[] MergeDisableBones()
        {
            bones = bones.Where(b => b != null).ToList();
            var table = Enumerable.Range(0, bones.Count).ToArray();
            var newBones = bones.Where(b=>b.gameObject.activeInHierarchy).ToList();
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].gameObject.activeInHierarchy == false)
                {
                    var p = bones[i];
                    while (p!=null)
                    {
                        p = p.parent;
                        var index = newBones.FindIndex(b => b == p);
                        if (index!=-1)
                        {
                            table[i] = index;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < boneWeights.Count; i++)
            {
                boneWeights[i] = new BoneWeight()
                {
                    boneIndex0 = table[boneWeights[i].boneIndex0],
                    boneIndex1 = table[boneWeights[i].boneIndex1],
                    boneIndex2 = table[boneWeights[i].boneIndex2],
                    boneIndex3 = table[boneWeights[i].boneIndex3],
                    weight0 = boneWeights[i].weight0,
                    weight1 = boneWeights[i].weight1,
                    weight2 = boneWeights[i].weight2,
                    weight3 = boneWeights[i].weight3,
                };
            }

            var newBindPoses = new List<Matrix4x4>();
            foreach (var bone in newBones)
            {
                var bindPose = bone.worldToLocalMatrix * rendBone.transform.localToWorldMatrix;
                newBindPoses.Add(bindPose);
            }
            
            bones = newBones;
            bindPoses = newBindPoses;
            return bones.ToArray();
        }

        /// <summary>
        /// メッシュの基準となるボーンを差し替える
        /// </summary>
        /// <param name="target"></param>
        /// <param name="origin"></param>
        public Transform[] ChangeBones(GameObject target, GameObject origin, bool nameMatch = false,Action<Transform> additiveBone = null,Action<Transform> changeRootBone = null)
        {
            var boneTable = new Dictionary<Transform, Transform>();
            boneTable.Add(origin.transform,target.transform);
            var targetBones = target.GetBones();
            var originBones = origin.GetBones();
            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (targetBones[(int)bone] != null && originBones[(int)bone] != null)
                {
                    boneTable.Add(originBones[(int)bone],targetBones[(int)bone]);
                }
            }

            var newBones = new List<Transform>();
            foreach (var bone in bones)
            {
                if(bone == null) continue;
                if (boneTable.ContainsKey(bone))
                {
                    newBones.Add(boneTable[bone]);
                }
                else
                {
                    var newBone = GenerateBone(ref boneTable, bone, 
                        nameMatch ? target.transform : null);
                    newBones.Add(newBone);
                    additiveBone?.Invoke(newBone);
                }
            }

            var newBindPoses = new List<Matrix4x4>();
            foreach (var bone in newBones)
            {
                var bindPose = bone.worldToLocalMatrix * rendBone.transform.localToWorldMatrix;
                newBindPoses.Add(bindPose);
            }

            if (rootBone)
            {
                if (boneTable.ContainsKey(rootBone))
                {
                    rootBone = boneTable[rootBone];
                    changeRootBone?.Invoke(rootBone);
                }
            }

            bones = newBones;
            bindPoses = newBindPoses;
            return bones.ToArray();
        }

        /// <summary>
        /// ボーンを作成する
        /// </summary>
        /// <param name="boneTable"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        Transform GenerateBone(
            ref Dictionary<Transform, Transform> boneTable,
            Transform origin,Transform newRoot = null)
        {
            if (origin == null) return newRoot;
            // is not HumanBone
            GameObject g = null;
            if (boneTable.ContainsKey(origin))
            {
                // 既にボーンがリストにある場合
                g = boneTable[origin].gameObject;
            }
            else
            {
                if (newRoot != null)
                {
                    // ボーンのname match
                    var t = newRoot.FindInChildren(origin.name);
                    if (t != null)
                    {
                        g = t.gameObject;
                    }
                }

                if (g == null)
                {
                    // ボーンの作成
                    g = new GameObject();
                    g.name = origin.gameObject.name;
                    g.transform.localPosition = origin.localPosition;
                    g.transform.localRotation = origin.localRotation;
                    g.transform.localScale = origin.localScale;
                    // 親探し
                    boneTable.Add(origin,g.transform);
                    g.transform.SetParent(GenerateBone(ref boneTable,origin.parent,newRoot),false);
                }
                else
                {
                    boneTable.Add(origin,g.transform);
                }
            
            }

            return g.transform;
        }

        /// <summary>
        /// メッシュの頂点位置をランダム化
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public string Encryption(Material mat)
        {
            if (mat != null)
            {
                for(int i=0;i<materials.Count;i++)
                {
                    var m = new Material(mat);
                    m.SetTexture("_MainTex",materials[i].GetTexture("_MainTex"));
                    materials[i] = m;
                }
            }
            
            string rands = "";
            var randXs = new List<float>();
            var randYs = new List<float>();
            var randZs = new List<float>();
            for (int i = 0; i < 100; i++)
            {
                randXs.Add(Mathf.Floor(Random.Range(-1f, 1f) * 100f)*0.001f);
                randYs.Add(Mathf.Floor(Random.Range(-1f, 1f) * 100f)*0.001f);
                randZs.Add(Mathf.Floor(Random.Range(-1f, 1f) * 100f)*0.001f);
            }

            int ran = 0;
            for (int i = 0; i < vertexs.Count; i++)
            {
                vertexs[i] += new Vector3(randXs[ran],randYs[ran],randZs[ran]);
                ran++;
                if (ran == 100) ran = 0;
            }

            rands += "static float randXs[100] = {";
            for (int i = 0; i < 100; i++)
            {
                rands += randXs[i];
                rands += ",";
            }
            rands += "};";
            rands += Environment.NewLine;

            rands += "static float randYs[100] = {";
            for (int i = 0; i < 100; i++)
            {
                rands += randYs[i];
                rands += ",";
            }
            rands += "};";
            rands += Environment.NewLine;

            rands += "static float randZs[100] = {";
            for (int i = 0; i < 100; i++)
            {
                rands += randZs[i];
                rands += ",";
            }
            rands += "};";
            rands += Environment.NewLine;
            
            return rands;
        }

        private List<Vector3> triangleNormals;
        public List<Vector3> CaltulateTriangleNormal()
        {
            var ns = new List<Vector3>();
            foreach (var tris in triangles)
            {
                for (int i = 0; i < tris.Count; i += 3)
                {
                    ns.Add(
                        Vector3.Cross(
                            Vector3.Normalize(vertexs[tris[i+1]]-vertexs[tris[i+0]]),
                            Vector3.Normalize(vertexs[tris[i+2]]-vertexs[tris[i+0]])
                            ));
                }
            }

            return ns;
        }
        private Vector3[] vertexnormals;
        public Vector3[] CaltulateVertexNormal()
        {
            var ns = new Vector3[vertexs.Count];
            for (int i = 0; i < vertexs.Count; i++)
            {
                if(ns[i] != Vector3.zero) continue;
                var n = Vector3.zero;
                var vs = GetOverlap(i);
                var ts = GetTriangleList(vs, null,1);
                foreach (var t in ts)
                {
                    var tv = GetVertexIndex(t);
                    var a1 = Vector3.zero;
                    var a2 = Vector3.zero;
                    if (vs.Contains(tv[0]))
                    {
                        a1 = Vector3.Normalize(vertexs[tv[1]] - vertexs[tv[0]]);
                        a2 = Vector3.Normalize(vertexs[tv[2]] - vertexs[tv[0]]);
                    }
                    else
                    if (vs.Contains(tv[1]))
                    {
                        a1 = Vector3.Normalize(vertexs[tv[2]] - vertexs[tv[1]]);
                        a2 = Vector3.Normalize(vertexs[tv[0]] - vertexs[tv[1]]);
                    }
                    else
                    if (vs.Contains(tv[2]))
                    {
                        a1 = Vector3.Normalize(vertexs[tv[0]] - vertexs[tv[2]]);
                        a2 = Vector3.Normalize(vertexs[tv[1]] - vertexs[tv[2]]);
                    }
                    n += Vector3.Cross(a1,a2) * Vector3.Dot(a1,a2);
                }

                foreach (var v in vs)
                {
                    ns[v] = n.normalized;
                }
            }

            return ns;
        }


        // モデルの形に合わせてnormalを修正する(うまく動かない)
        public void ApplyNormalsDiff()
        {
            if (vertexnormals != null)
            {
                var newNormals = CaltulateVertexNormal();
                var oldNormals = vertexnormals;
                for (int i = 0; i < vertexs.Count; i++)
                {
                    if (newNormals[i] != oldNormals[i])
                    {
                        normals[i] = Quaternion.FromToRotation(oldNormals[i], newNormals[i]) * normals[i];
                    }
                }
                vertexnormals = newNormals;
            }
            else
            {
                vertexnormals = CaltulateVertexNormal();
            }
        }
        
        public void ApplyNormals()
        {
            if (vertexnormals != null)
            {
                var newNormals = CaltulateVertexNormal();
                for (int i = 0; i < vertexs.Count; i++)
                {
                    normals[i] = newNormals[i];
                }
                vertexnormals = newNormals;
            }
        }

        /// <summary>
        /// 現在の頂点位置をキャッシュに保存する
        /// </summary>
        public void AddCaches()
        {
            if (vertexsCaches.Count != 0)
            {
                if (vertexs.Count != vertexsCaches[0].Count)
                {
                    if (vertexs.Count > vertexsCaches[0].Count)
                    {
                        vertexsCaches[0].AddRange(vertexs.GetRange(vertexsCaches[0].Count,vertexs.Count-vertexsCaches[0].Count));
                    }
                    else
                    {
                        vertexsCaches = new List<List<Vector3>>();
                    }
                }
                else
                {
                    if (vertexsCaches.Count >= maxCaches)
                    {
                        vertexsCaches.RemoveAt(1);
                    }

                    while(currentCacheIndex+1<vertexsCaches.Count)
                    {
                        vertexsCaches.RemoveAt(currentCacheIndex + 1);
                    }
                }
            }

            vertexsCaches.Add(vertexs.ToList());
            currentCacheIndex = vertexsCaches.Count-1;
            //ApplyNormalsDiff();
            //ApplyNormals();
        }

        /// <summary>
        /// 初期キャッシュの頂点位置を編集する
        /// BlendShape化の基準点となる
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="offset"></param>
        public void TransformAtDefault(List<Vector3> mesh,int offset)
        {
            AddCaches();
            var vs = vertexsCaches[0].ToList();
            for (int i = 0; i < mesh.Count; i++)
            {
                vs[i + offset] = mesh[i];
            }
            vertexsCaches.Insert(0,vs);
            while(currentCacheIndex+1<vertexsCaches.Count)
            {
                vertexsCaches.RemoveAt(1);
            }
        }

        public void TransformUV(
            Vector2 scale, 
            Vector2 position,
            List<int> verts = null)
        {
            if (verts != null)
            {
                foreach (var vert in verts)
                {
                    uvs[0][vert] = uvs[0][vert] * scale + position;
                }
            }
            else
            {
                for (int i = 0; i < vertexs.Count; i++)
                {
                    uvs[0][i] = uvs[0][i] * scale + position;
                }
            }
        }

        /// <summary>
        /// キャッシュを1つ戻す
        /// </summary>
        public void UndoCaches()
        {
            if (CanUndo())
            {
                currentCacheIndex --;
                TransformMesh(vertexsCaches[currentCacheIndex].ToArray());
                Create(false);
            }
        }

        /// <summary>
        /// Undoボタンが押せるかどうか
        /// </summary>
        /// <returns></returns>
        public bool CanUndo()
        {
            return currentCacheIndex > 0;
        }
        
        /// <summary>
        /// キャッシュを一つ進める
        /// </summary>
        public void RedoCaches()
        {
            if (CanRedo())
            {
                currentCacheIndex++;
                TransformMesh(vertexsCaches[currentCacheIndex].ToArray());
                Create(false);
            }
        }
        
        /// <summary>
        /// Redoボタンが押せるかどうか
        /// </summary>
        /// <returns></returns>
        public bool CanRedo()
        {
            return vertexsCaches.Count > currentCacheIndex + 1;
        }

        public void ResetCaches()
        {
            currentCacheIndex = 0;
        }

        /// <summary>
        /// 頂点の初期配置
        /// </summary>
        /// <returns></returns>
        public List<Vector3> DefaultVertexs()
        {
            if (vertexsCaches.Count == 0)
            {
                return vertexs;
            }
            else
            {
                return vertexsCaches[0];
            }
        }

        /// <summary>
        /// 現在の頂点配置
        /// </summary>
        /// <returns></returns>
        public List<Vector3> EditVertexs()
        {
            return vertexs;
        }
        
        /// <summary>
        /// 出力済みメッシュの取得
        /// </summary>
        /// <returns></returns>
        public Mesh GetMesh()
        {
            if (asset == null) Create();
            return asset;
        }

        public Vector3 GetPosition(int index)
        {
            if (index < VertexsCount())
            {
                return vertexs[index];
            }
            return Vector3.zero;
        }

        public Vector3 GetNormal(int index)
        {
            if (index < VertexsCount())
            {
                return normals[index];
            }
            return Vector3.up;
        }

        public Vector3 GetTangent(int index)
        {
            if (index < VertexsCount())
            {
                return tangents[index];
            }
            return Vector3.right;
        }
        
        public Color GetColor(int index)
        {
            if (index < VertexsCount())
            {
                return colors[index];
            }
            return Color.white;
        }
        
        public Vector2[] GetUVs(int index)
        {
            if (index < VertexsCount())
            {
                return uvs.Select(uv=>uv[index]).ToArray();
            }
            return Enumerable.Range(0, 8).Select(_ => Vector2.zero).ToArray();
        }

        public KeyValuePair<int, float>[] GetWeightData(int index)
        {
            var w = new List<KeyValuePair<int, float>>();
            if (index < VertexsCount())
            {
                w.Add(new KeyValuePair<int,float>(boneWeights[index].boneIndex0,boneWeights[index].weight0));
                w.Add(new KeyValuePair<int,float>(boneWeights[index].boneIndex1,boneWeights[index].weight1));
                w.Add(new KeyValuePair<int,float>(boneWeights[index].boneIndex2,boneWeights[index].weight2));
                w.Add(new KeyValuePair<int,float>(boneWeights[index].boneIndex3,boneWeights[index].weight3));
                return w.ToArray();
            }
            w.Add(new KeyValuePair<int,float>(0,0f));
            w.Add(new KeyValuePair<int,float>(1,0f));
            w.Add(new KeyValuePair<int,float>(2,0f));
            w.Add(new KeyValuePair<int,float>(3,0f));
            return w.ToArray();
        }

        public void SetRawData(int index, Vector3? pos, Vector3? nor, Vector3? tan, Color? col, Vector2[] uv,
            KeyValuePair<int, float>[] weights)
        {
            if (index < VertexsCount())
            {
                vertexs[index] = pos ?? vertexs[index];
                normals[index] = nor ?? normals[index];
                tangents[index] = tan ?? tangents[index];
                colors[index] = col ?? colors[index];
                if (uv != null)
                {
                    for (int i = 0; i < uv.Length && i < uvs.Length; i++)
                    {
                        uvs[i][index] = uv[i];
                    }
                }
                if (weights != null)
                {
                    if (weights.Length == 4)
                    {
                        boneWeights[index] = new BoneWeight()
                        {
                            boneIndex0 = weights[0].Key,
                            boneIndex1 = weights[1].Key,
                            boneIndex2 = weights[2].Key,
                            boneIndex3 = weights[3].Key,
                            weight0 = weights[0].Value,
                            weight1 = weights[1].Value,
                            weight2 = weights[2].Value,
                            weight3 = weights[3].Value,
                        };
                    }
                }
            }
        }

        public Vector2 GetUVDelta(int submesh, int triangle, Vector3 wp)
        {
            var vs = new List<int>()
            {
                triangles[submesh][triangle * 3 + 0],
                triangles[submesh][triangle * 3 + 1],
                triangles[submesh][triangle * 3 + 2],
            };
            var ws = MeshUtil.ComputeBasis(wp, vs.Select(v => vertexs[v]).ToList());
            var wuv = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),ws);
            return wuv;
        }
        
        public float GetUVdelta(int submesh, int triangle, Vector3 wp)
        {
            var vs = new List<int>()
            {
                triangles[submesh][triangle * 3 + 0],
                triangles[submesh][triangle * 3 + 1],
                triangles[submesh][triangle * 3 + 2],
            };

            var ps = vs.Select(v => vertexs[v]).ToList();
            ps.Add(ps[ps.Count-1]);
            var us = vs.Select(v => uvs[0][v]).ToList();
            us.Add(us[us.Count-1]);

            float pd = 0f;
            float ud = 0f;
            for (int i = 0; i < vs.Count; i++)
            {
                pd += Vector3.Distance(ps[i], ps[i + 1]);
                ud += Vector3.Distance(us[i], us[i + 1]);
            }

            return ud / pd;
        }

        public Vector2 GetUVAxi(int submesh, int triangle, Vector3? up = null)
        {
            var vs = new List<int>()
            {
                triangles[submesh][triangle * 3 + 0],
                triangles[submesh][triangle * 3 + 1],
                triangles[submesh][triangle * 3 + 2],
            };
            
            var ps = vs.Select(v => vertexs[v]).ToList();
            ps.Add(ps[ps.Count-1]);

            float pd = 0f;
            for (int i = 0; i < vs.Count; i++)
            {
                pd += Vector3.Distance(ps[i], ps[i + 1]);
            }

            var normal = Vector3.Cross(
                Vector3.Normalize(vertexs[vs[1]] - vertexs[vs[0]]),
                Vector3.Normalize(vertexs[vs[2]] - vertexs[vs[0]]));
            var axi1 = Vector3.Normalize(Vector3.Cross(normal, RendBone.InverseTransformDirection(up ?? Vector3.up)));
            var axi2 = Vector3.Normalize(Vector3.Cross(normal, RendBone.InverseTransformDirection(up ?? Vector3.up)));
            var wp = ComputeWeightPoint(vs);
            var w0 = MeshUtil.ComputeBasis(wp, vs.Select(v => vertexs[v]).ToList());
            var w1 = MeshUtil.ComputeBasis(wp+axi1*pd*0.01f, vs.Select(v => vertexs[v]).ToList());
            var w2 = MeshUtil.ComputeBasis(wp+axi2*pd*0.01f, vs.Select(v => vertexs[v]).ToList());
            Vector2 uv0 = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),w0);
            Vector2 uv1 = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),w1);
            Vector2 uv2 = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),w2);
            var uvaxi1 = uv1 - uv0;
            var uvaxi2 = uv2 - uv0;
            return new Vector2(
                uvaxi1.normalized.x * Math.Sign(Vector2.Dot(uvaxi1, Vector2.right)),
                uvaxi1.normalized.y * Math.Sign(Vector2.Dot(uvaxi2, Vector2.up)));;
        }
        
        public Vector2 GetUVAspect(int submesh, int triangle, Vector3? up = null)
        {
            var vs = new List<int>()
            {
                triangles[submesh][triangle * 3 + 0],
                triangles[submesh][triangle * 3 + 1],
                triangles[submesh][triangle * 3 + 2],
            };
            
            var ps = vs.Select(v => vertexs[v]).ToList();
            ps.Add(ps[ps.Count-1]);

            float pd = 0f;
            for (int i = 0; i < vs.Count; i++)
            {
                pd += Vector3.Distance(ps[i], ps[i + 1]);
            }

            var normal = Vector3.Cross(
                Vector3.Normalize(vertexs[vs[1]] - vertexs[vs[0]]),
                Vector3.Normalize(vertexs[vs[2]] - vertexs[vs[0]]));
            var axi1 = Vector3.Normalize(Vector3.Cross(normal, RendBone.InverseTransformDirection(up ?? Vector3.up)));
            var axi2 = Vector3.Normalize(Vector3.Cross(normal, RendBone.InverseTransformDirection(up ?? Vector3.up)));
            var wp = ComputeWeightPoint(vs);
            var w0 = MeshUtil.ComputeBasis(wp, vs.Select(v => vertexs[v]).ToList());
            var w1 = MeshUtil.ComputeBasis(wp+axi1*pd*0.01f, vs.Select(v => vertexs[v]).ToList());
            var w2 = MeshUtil.ComputeBasis(wp+axi2*pd*0.01f, vs.Select(v => vertexs[v]).ToList());
            Vector2 uv0 = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),w0);
            Vector2 uv1 = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),w1);
            Vector2 uv2 = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),w2);
            var uvaxi1 = uv1 - uv0;
            var uvaxi2 = uv2 - uv0;
            return new Vector2(Vector2.Dot(uvaxi1, Vector2.right), Vector2.Dot(uvaxi2, Vector2.up));
        }
        
        /// <summary>
        /// NeshRendererとして出力する
        /// </summary>
        /// <param name="name"></param>
        /// <param name="collider"></param>
        /// <returns></returns>
        public GameObject ToMesh(string name = "CreateMesh", bool collider = false,Transform parent = null)
        {
            var m = new GameObject(name);
            var mf = m.AddComponent<MeshFilter>();
            m.transform.SetParent(parent ?? rendBone);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;;
            m.transform.localScale = Vector3.one;
            mf.sharedMesh = parent ? Create(false) : GetMesh();

            if (collider)
            {
                var mc = m.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }

            var mr = m.AddComponent<MeshRenderer>();
            mr.materials = materials.ToArray();

            return m;
        }
        
        /// <summary>
        /// SkinnedMeshとして出力する
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject ToSkinMesh(string name = "CreateMesh",Transform parent = null)
        {
            var m = new GameObject(name);
            var sm = m.AddComponent<SkinnedMeshRenderer>();
            m.transform.SetParent(parent ?? rendBone);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;;
            sm.sharedMesh = parent ? Create(false) : GetMesh();
            sm.bones = bones.ToArray();
            sm.rootBone = rootBone;

            sm.sharedMaterials = materials.ToArray();

            return m;
        }

        /// <summary>
        /// 頂点カラー付き，部分非表示のMeshを出力する
        /// </summary>
        /// <param name="select">赤く塗る頂点</param>
        /// <param name="controll">表示する頂点</param>
        /// <param name="origin">Meshインスタンス(あれば)</param>
        /// <param name="wp">重心移動</param>
        /// <returns></returns>
        public Mesh CreateEditMesh(List<int> select = null,List<int> controll = null,Mesh origin = null,Vector3? wp = null)
        {
            if (origin == null)
            {
                origin = Mesh.Instantiate(Create(false,false));
            }

            if (origin.vertexCount != vertexs.Count)
            {
                origin = Mesh.Instantiate(Create(false,false));
            }

            if (wp != null)
            {
                Vector3 vec = wp ?? Vector3.zero;
                var vs = new List<Vector3>();
                foreach (var v in vertexs)
                {
                    vs.Add(v+vec);
                }
                origin.SetVertices(vs);
            }
            else
            {
                origin.SetVertices(vertexs);
            }

            var cs = Enumerable.Range(0, origin.vertexCount).Select(_ => Color.white).ToList();
            if (select != null)
            {
                for (int i = 0; i < select.Count; i++)
                {
                    cs[select[i]] = Color.red;
                }
            }
            origin.SetColors(cs);
            
            if (controll != null)
            {
                var trisIndex = GetTriangleList(controll, (i, t) =>
                {
                    origin.SetTriangles(t.ToArray(),i);
                });
            }
            
            var bounds = origin.bounds;
            bounds.center = Vector3.zero;
            bounds.extents = Vector3.one;
            bounds.Expand( 100f );
            origin.bounds = bounds;
            
            return origin;
        }

        public void DuplicateSubMesh(int submeshID)
        {
            if (submeshID < 0 || triangles.Count < submeshID) return;
            AddSubMesh(
                triangles[submeshID].ToList(),
                materials[submeshID],
                bones[submeshID],
                GetTriangleOffset(submeshID));
        }
        
        public Mesh CreateSubMesh(int submeshID,bool skinning = false)
        {
            if (submeshID < 0 || triangles.Count < submeshID) return null;
            
            Mesh combinedMesh = new Mesh();
            combinedMesh.SetVertices(vertexs);
            combinedMesh.SetColors(colors);
            
            for (int i = 0; i < 8; i++)
            {
                combinedMesh.SetUVs(i, uvs[i]);
            }
            
            combinedMesh.subMeshCount = 1;
            combinedMesh.SetTriangles(triangles[submeshID].ToArray(),0);

            if (skinning)
            {
                combinedMesh.bindposes = bindPoses.ToArray();
                combinedMesh.boneWeights = boneWeights.ToArray();

                combinedMesh.ClearBlendShapes();
                foreach (var blendShape in blendShapes)
                {
                    blendShape.Apply(ref combinedMesh);
                }
            }

            combinedMesh.SetNormals(normals);
            combinedMesh.SetTangents(tangents);
            
            return combinedMesh;
        }
        
        /// <summary>
        /// Meshとして出力する
        /// </summary>
        /// <param name="caches">キャッシュするかどうか</param>
        /// <returns></returns>
        public Mesh Create(bool caches = true,bool createBlendShape = true)
        {
            Mesh combinedMesh = new Mesh();
            combinedMesh.SetVertices(vertexs);
            if (vertexs.Count > 65535)
            {
                combinedMesh.indexFormat = IndexFormat.UInt32;
            }
            else
            {
                combinedMesh.indexFormat = IndexFormat.UInt16;
            }
            combinedMesh.SetColors(colors);
            
            for (int i = 0; i < 8; i++)
            {
                if(uvs[i]!=null) combinedMesh.SetUVs(i, uvs[i]);
            }
            
            combinedMesh.subMeshCount = triangles.Count;
            for (int i=0; i<triangles.Count; i++)
            {
                combinedMesh.SetTriangles(triangles[i].ToArray(),i);
            }

            combinedMesh.bindposes = bindPoses.ToArray();
            combinedMesh.boneWeights = boneWeights.ToArray();

            if (createBlendShape)
            {
                combinedMesh.ClearBlendShapes();
                foreach (var blendShape in blendShapes)
                {
                    blendShape.Apply(ref combinedMesh);
                }
            }

            combinedMesh.SetNormals(normals);
            combinedMesh.SetTangents(tangents);
            if (IsRecalculateNormals)
            {
                combinedMesh.RecalculateNormals();
                combinedMesh.RecalculateTangents();
            }

            asset = combinedMesh;
            
            try
            {
                OnReloadMesh?.Invoke(combinedMesh);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            
            if(caches) AddCaches();
            
            return combinedMesh;
        }
        
        /// <summary>
        /// 偽のメッシュを出力する
        /// SkinedMeshの初期化用
        /// </summary>
        /// <param name="combinedMesh"></param>
        /// <param name="setAsAsset"></param>
        /// <returns></returns>
        public Mesh Create(Mesh combinedMesh,bool setAsAsset = false)
        {
            if(setAsAsset) asset = combinedMesh;
            try
            {
                OnReloadMesh?.Invoke(combinedMesh);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            
            AddCaches();
            
            return combinedMesh;
        }

        /// <summary>
        /// 現在の変更をBlendShapeとして保存する
        /// </summary>
        /// <param name="blendshapeName"></param>
        /// <returns></returns>
        public Mesh SaveAsBlendshape(string blendshapeName)
        {
            if (IsRecalculateBlendShapeNormals)
            {
                var mesh = Create(false);
                blendShapes.Add(new BlendShapeData(blendshapeName,DefaultVertexs(),EditVertexs(),mesh));
            }
            else
            {
                blendShapes.Add(new BlendShapeData(blendshapeName,DefaultVertexs(),EditVertexs()));
            }
            TransformMesh(DefaultVertexs().ToArray());
            return Create();
        }

        /// <summary>
        /// 同一マテリアルのサブメッシュを結合する
        /// </summary>
        public void CombineMesh()
        {
            var newMats = materials.Distinct().ToList();
            var newTriangles = new List<List<int>>();
            var newTrans = new List<Transform>();
            
            foreach (var nm in newMats)
            {
                var ts = new List<int>();
                for (int i = 0; i < triangles.Count; i++)
                {
                    if (materials[i] == nm)
                    {
                        ts.AddRange(triangles[i]);
                    }
                }

                newTriangles.Add(ts);
                newTrans.Add(null);
            }

            materials = newMats;
            triangles = newTriangles;
            meshTransforms = newTrans;
        }

        /// <summary>
        /// マテリアルを結合する
        /// </summary>
        /// <param name="path"></param>
        public void MaterialAtlas(string path = null)
        {
            var newTriangles = new List<List<int>>();
            var newMats = new List<Material>();
            var newTrans = new List<Transform>();
            
            var shaders = materials.Select(m => m.shader).Distinct().ToArray();
            foreach (var shader in shaders)
            {
                Debug.Log(shader.name);
                var mats = new List<Material>();
                var tris = new List<List<int>>();
                for (int i = 0; i < materials.Count; i++)
                {
                    if (materials[i].shader == shader)
                    {
                        if (meshTransforms[i]!=null)
                        {
                            meshTransforms[i] = null;
                        }
                        mats.Add(materials[i]);
                        tris.Add(triangles[i]);
                        newTrans.Add(null);
                    }
                }
                
                int tilling = 1;
                while (mats.Count > tilling * tilling)
                {
                    tilling++;
                }
                var delta = 1f / (float) tilling;
                int subMeshIndex = 0;
                var texs = new List<Texture2D>();
                for (int column = tilling-1; column >= 0 && subMeshIndex < mats.Count; column--)
                {
                    for (int row = 0; row < tilling && subMeshIndex < mats.Count; row++)
                    {
                        var pos = new Vector2(row*delta, column*delta);
                        var vs = tris[subMeshIndex].Distinct().ToArray();
                        foreach (var v in vs)
                        {
                            for (int i = 0; i < uvs.Length; i++)
                            {
                                uvs[i][v] = pos + new Vector2(uvs[i][v].x/(float) tilling,uvs[i][v].y/(float) tilling);
                            }
                        }

                        var t = mats[subMeshIndex].GetTexture("_MainTex") as Texture2D;
                        if(t==null) t = new Texture2D(1,1);
                        texs.Add(t);
                        subMeshIndex++;
                    }
                }
                
                var newTriangle = new List<int>();
                foreach (var tri in tris)
                {
                    foreach (var t in tri)
                    {
                        newTriangle.Add(t);
                    }
                }
                newTriangles.Add(newTriangle);

                if (!string.IsNullOrWhiteSpace(path))
                {
                    var tex = TextureCombinater.CombinateSaveTexture(texs.ToArray(),
                        path + "_texAtlas" + ".png",
                        tilling);
                    var mat = new Material(shader);
                    mat.SetTexture("_MainTex",tex);
                    AssetDatabase.CreateAsset(mat,path + "_matAtlas.mat");
                    newMats.Add(mat);
                }
                else
                {
                    var tex = TextureCombinater.CombinateSaveTexture(texs.ToArray(),path,tilling);
                    var mat = new Material(shader);
                    mat.SetTexture("_MainTex",tex);
                    newMats.Add(mat);
                }
            }
            triangles = newTriangles;
            materials = newMats;
            meshTransforms = newTrans;
        }

        /// <summary>
        /// メッシュを結合する
        /// </summary>
        /// <param name="path"></param>
        public void ForceCombine(string path = null)
        {
            var newTriangles = new List<List<int>>();
            var newTrans = new List<Transform>();
            
            var ts = new List<int>();
            for (int i = 0; i < triangles.Count; i++)
            {
                ts.AddRange(triangles[i]);
            }

            newTriangles.Add(ts);
            newTrans.Add(null);

            materials = new List<Material>(){materials[0]};
            triangles = newTriangles;
            meshTransforms = newTrans;
        }

        public int GenerateWeightVertex(List<int> vs,Vector3? wp_null = null)
        {
            var wp = wp_null ?? ComputeWeightPoint(vs);
            var ws = MeshUtil.ComputeBasis(wp,vs.Select(v => vertexs[v]).ToList());
            var wn = MeshUtil.Average(vs.Select(v => normals[v]).ToList(),ws);
            var wt = MeshUtil.Average(vs.Select(v => tangents[v]).ToList(),ws);
            var wc = MeshUtil.Average(vs.Select(v => colors[v]).ToList(),ws);
            var wu = MeshUtil.Average(vs.Select(v => uvs[0][v]).ToList(),ws);
            var ww = MeshUtil.Average(vs.Select(v => boneWeights[v]).ToList(),ws);
            AddVertex(wp,wn,wt,wc,new Vector2[8]{wu,wu,wu,wu,wu,wu,wu,wu},ww);
            AddBlendshapeIndex(vs.ToArray(),ws.ToArray());
            return vertexs.Count - 1;
        }

        public void Decimate(int triangle)
        {
            var vs = GetVertexIndex(triangle).Distinct().ToList();
            if (vs.Count == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    vs.AddRange(GetOverlap(vs[0]));
                    vs.AddRange(GetOverlap(vs[1]));
                    vs.AddRange(GetOverlap(vs[2]));
                    vs = vs.Distinct().ToList();
                }

                int vi = GenerateWeightVertex(vs);
                int index = 0;
                for (int i = 0; i < triangles.Count; i++)
                {
                    for (int j = 0; j < triangles[i].Count; j+=3)
                    {
                        // 削除予定triangleでは頂点の入れ替えを行わない
                        if (index != triangle)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                if (vs.Contains(triangles[i][j + k]))
                                {
                                    triangles[i][j + k] = vi;
                                }
                            }
                        }

                        index++;
                    }
                }
                RemoveVertexesTriangles(vs,false,true);
            }
        }

        public void Subdivision(int triangle,Vector3? wp_null = null,bool isSelectVertex = false)
        {
            var vs = GetVertexIndex(triangle).Distinct().ToList();
            if (vs.Count == 3)
            {
                var wp = wp_null ?? ComputeWeightPoint(vs);
                var ws = MeshUtil.ComputeBasis(wp,vs.Select(v => vertexs[v]).ToList());
                if (ws.Any(w => w > 0.9f && isSelectVertex))
                {
                    // 頂点に近い場合，即リターン
                }
                else
                if (ws.Any(w => w < 0.1f) && isSelectVertex)
                {
                    // 辺に近い場合，辺分割
                    var edge = vs.ToList();
                    for (int i = 0; i < 3; i++)
                    {
                        if (ws[i] < 0.1f)
                        {
                            edge.RemoveAt(i);
                        }
                    }

                    var vid = GenerateWeightVertex(edge, wp_null);
                    var triangles = GetTriangleList(edge, null, 2);
                    while (triangles.Count > 0)
                    {
                        var sid = GetSubmeshID(triangles[0]);
                        var vids = GetVertexIndex(triangles[0]);
                        if (vids.Count == 3)
                        {
                            RemoveTriangle(triangles[0]);
                            var acuteIndex = vids.FindIndex(v => !edge.Contains(v));
                            if (acuteIndex != -1)
                            {
                                var acute = vids[acuteIndex];

                                if (vids.FindIndex(v => v == acute) + 1 == vids.FindIndex(v => v == edge[0]) ||
                                    vids.FindIndex(v => v == acute) - 2 == vids.FindIndex(v => v == edge[0]))
                                {
                                    AddTriangle(sid,new List<int>{acute,edge[0],vid});
                                }
                                else
                                {
                                    AddTriangle(sid,new List<int>{acute,vid,edge[0]});
                                }
                                if (vids.FindIndex(v => v == acute) + 1 == vids.FindIndex(v => v == edge[1]) ||
                                    vids.FindIndex(v => v == acute) - 2 == vids.FindIndex(v => v == edge[1]) )
                                {
                                    AddTriangle(sid,new List<int>{acute,edge[1],vid});
                                }
                                else
                                {
                                    AddTriangle(sid,new List<int>{acute,vid,edge[1]});
                                }
                            }
                        }
                        triangles = GetTriangleList(edge, null, 2);
                    }
                }
                else
                {
                    // ポリゴン分割
                    var vid = GenerateWeightVertex(vs, wp_null);
                    var sid = GetSubmeshID(triangle);
                    RemoveTriangle(triangle);
                    AddTriangle(sid,new List<int>{vs[0],vs[1],vid});
                    AddTriangle(sid,new List<int>{vs[1],vs[2],vid});
                    AddTriangle(sid,new List<int>{vs[2],vs[0],vid});
                }
            }

        }

        public void CopyBoneWeight(SkinnedMeshRenderer[] rends)
        {
            var meshes = new List<Mesh>();
            var tables = new List<int[]>();
            foreach (var rend in rends)
            {
                var mesh = Mesh.Instantiate(rend.sharedMesh);
                //rend.BakeMesh(mesh);
                meshes.Add(mesh);
                tables.Add(AddBones(rend.bones));
            }

            for (int i = 0; i < vertexs.Count; i++)
            {
                float distance = 0.1f;
                for (int j = 0; j < rends.Length; j++)
                {
                    for (int k = 0; k < meshes[j].vertexCount; k++)
                    {
                        var oPos = rootBone.InverseTransformPoint(vertexs[i]);
                        var rPos = rends[j].rootBone.InverseTransformPoint(meshes[j].vertices[k]);
                        var dist = Vector3.Distance(oPos, rPos);
                        if (dist < distance)
                        {
                            distance = dist;
                            boneWeights[i] = new BoneWeight()
                            {
                                boneIndex0 = tables[j][meshes[j].boneWeights[k].boneIndex0],
                                boneIndex1 = tables[j][meshes[j].boneWeights[k].boneIndex1],
                                boneIndex2 = tables[j][meshes[j].boneWeights[k].boneIndex2],
                                boneIndex3 = tables[j][meshes[j].boneWeights[k].boneIndex3],
                                weight0 = meshes[j].boneWeights[k].weight0,
                                weight1 = meshes[j].boneWeights[k].weight1,
                                weight2 = meshes[j].boneWeights[k].weight2,
                                weight3 = meshes[j].boneWeights[k].weight3,
                            };
                        }
                        
                    }
                }
            }

        }

        public float GetSize(int triangle)
        {
            var vs = GetVertexIndex(triangle);
            var a = Vector3.Distance(vertexs[vs[0]], vertexs[vs[1]]);
            var b = Vector3.Distance(vertexs[vs[1]], vertexs[vs[2]]);
            var c = Vector3.Distance(vertexs[vs[2]], vertexs[vs[0]]);
            var s = (a + b + c) / 2f;
            return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        /// <summary>
        /// Meshをアセットとして保存する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Mesh Save(string path,bool subAsset = false)
        {
            var combinedMesh = Create(false);
            if (subAsset)
            {
                AssetDatabase.AddObjectToAsset(combinedMesh,path);
            }
            else
            {
                AssetDatabase.CreateAsset(combinedMesh,path);
            }
            return combinedMesh;
        }
    }

    /// <summary>
    /// 便利関数用staticクラス
    /// </summary>
    public static class MeshUtil
    {
        public static List<Vector2>[] UVLists(this Mesh origin)
        {
            var o = new List<Vector2>[8];
            o[0] = origin.uv.ToList();
            o[1] = origin.uv2.ToList();
            o[2] = origin.uv3.ToList();
            o[3] = origin.uv4.ToList();
            o[4] = origin.uv5.ToList();
            o[5] = origin.uv6.ToList();
            o[6] = origin.uv7.ToList();
            o[7] = origin.uv8.ToList();
            for (int i = 0; i < 8; i++)
            {
                if(o[i].Count<origin.vertexCount) o[i].AddRange(Enumerable.Range(o[i].Count, origin.vertexCount - o[i].Count).Select(_=>Vector2.zero));
            }
            return o;
        }
        public static List<Vector2[]> UVArray(this Mesh origin)
        {
            var uvs = origin.UVLists();
            return Enumerable.Range(0,origin.vertexCount).
                Select(v=> 
                    new Vector2[8]{uvs[0][v],uvs[1][v],uvs[2][v],uvs[3][v],uvs[4][v],uvs[5][v],uvs[6][v],uvs[7][v]}).ToList();
        }

        public static Mesh GetMesh(this Renderer rend)
        {
            if (rend.GetType() == typeof(SkinnedMeshRenderer))
            {
                var mesh = rend as SkinnedMeshRenderer;
                return mesh.sharedMesh;
            }
            else
            if (rend.GetType() == typeof(MeshRenderer))
            {
                var mesh = rend.GetComponent<MeshFilter>() as MeshFilter;
                return mesh.sharedMesh;
            }

            return null;
        }
        
        public static void SetMesh(this Renderer rend,Mesh mesh)
        {
            if(rend == null) return;
            if (rend.GetType() == typeof(SkinnedMeshRenderer))
            {
                var m = rend as SkinnedMeshRenderer;
                m.sharedMesh = mesh;
            }
            else
            if (rend.GetType() == typeof(MeshRenderer))
            {
                var m = rend.GetComponent<MeshFilter>() as MeshFilter;
                m.sharedMesh = mesh;
            }
        }

        public static List<Vector3> GetCalculateNormals(this Mesh mesh)
        {
            var m = new Mesh();
            m.SetVertices(mesh.vertices.ToList());
            m.subMeshCount = mesh.subMeshCount;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                m.SetTriangles(mesh.GetTriangles(i),i);
            }
            var ns = Enumerable.Range(0,m.vertexCount).Select(_=>Vector3.zero).ToList();
            m.SetNormals(ns);
            m.RecalculateNormals();
            m.GetNormals(ns);
            return ns;
        }
        
        public static List<Vector4> GetCalculateTangents(this Mesh mesh)
        {
            var m = new Mesh();
            m.SetVertices(mesh.vertices.ToList());
            m.subMeshCount = mesh.subMeshCount;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                m.SetTriangles(mesh.GetTriangles(i),i);
            }
            var ts = Enumerable.Range(0,m.vertexCount).Select(_=>Vector4.zero).ToList();
            m.SetTangents(ts);
            m.RecalculateTangents();
            m.GetTangents(ts);
            return ts;
        }

        public static List<float> ComputeBasis(Vector3 pos,List<Vector3> vertex)
        {
            var output = new List<float>();
            if (vertex.Count == 3)
            {
                vertex.Add(vertex[0]);
                vertex.Add(vertex[1]);
                var normal = Vector3.Cross(
                    Vector3.Normalize(vertex[1] - vertex[0]),
                    Vector3.Normalize(vertex[2] - vertex[0]));
                normal = Vector3.Normalize(normal);

                for (int i = 0; i < 3; i++)
                {
                    var axi = Vector3.Cross(normal, vertex[i+1] - vertex[i+2]);
                    axi = Vector3.Normalize(axi);
                    var zero = Vector3.Dot(axi, vertex[i + 1]);
                    var one = Vector3.Dot(axi, vertex[i]);
                    var val = Vector3.Dot(axi, pos);
                    output.Add(Mathf.Clamp01((val - zero) / (one - zero)));
                }
            }
            
            // 正規化
            var dist = output.Sum();
            if(dist!=0) output = output.Select(o => o / dist).ToList();
            return output;
        }

        public static Vector2 Average(List<Vector2> ls,List<float> weights = null)
        {
            if (weights == null) weights = new List<float>();
            if (weights.Count != ls.Count) weights = Enumerable.Range(0, ls.Count).Select(_ => 1f / (float)ls.Count).ToList();

            Vector2 sum = Vector3.zero;
            for (int i = 0; i < ls.Count; i++)
            {
                sum += ls[i] * weights[i];
            }
            return sum;
        }
        public static Vector3 Average(List<Vector3> ls,List<float> weights = null)
        {
            if (weights == null) weights = new List<float>();
            if (weights.Count != ls.Count) weights = Enumerable.Range(0, ls.Count).Select(_ => 1f / (float)ls.Count).ToList();

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < ls.Count; i++)
            {
                sum += ls[i] * weights[i];
            }
            return sum;
        }
        public static Vector4 Average(List<Vector4> ls,List<float> weights = null)
        {
            if (weights == null) weights = new List<float>();
            if (weights.Count != ls.Count) weights = Enumerable.Range(0, ls.Count).Select(_ => 1f / (float)ls.Count).ToList();

            Vector4 sum = Vector4.zero;
            for (int i = 0; i < ls.Count; i++)
            {
                sum += ls[i] * weights[i];
            }
            return sum;
        }
        public static Color Average(List<Color> ls,List<float> weights = null)
        {
            if (weights == null) weights = new List<float>();
            if (weights.Count != ls.Count) weights = Enumerable.Range(0, ls.Count).Select(_ => 1f / (float)ls.Count).ToList();
            
            Color sum = Color.clear;
            for (int i = 0; i < ls.Count; i++)
            {
                sum += ls[i] * weights[i];
            }
            return sum;
        }
        public static BoneWeight Average(List<BoneWeight> ls,List<float> weights = null)
        {
            if (weights == null) weights = new List<float>();
            if (weights.Count != ls.Count) weights = Enumerable.Range(0, ls.Count).Select(_ => 1f / (float)ls.Count).ToList();

            var list = new Dictionary<int, float>();
            for (int i = 0; i < ls.Count; i++)
            {
                var bones = new int[]{ls[i].boneIndex0, ls[i].boneIndex1, ls[i].boneIndex2, ls[i].boneIndex3};
                var weight = new float[] {ls[i].weight0, ls[i].weight1, ls[i].weight2, ls[i].weight3};
                for (int j = 0; j < 4; j++)
                {
                    if (list.ContainsKey(bones[j]))
                    {
                        list[bones[j]] = list[bones[j]] + weight[j]*weights[i];
                    }
                    else
                    {
                        list.Add(bones[j],weight[j]*weights[i]);
                    }
                }
            }
            var sorted = list.OrderByDescending(l => l.Value).ToArray();
            return new BoneWeight()
            {
                boneIndex0 = sorted.Length>0 ? sorted[0].Key : 0,
                boneIndex1 = sorted.Length>1 ? sorted[1].Key : 0,
                boneIndex2 = sorted.Length>2 ? sorted[2].Key : 0,
                boneIndex3 = sorted.Length>3 ? sorted[3].Key : 0,
                weight0 = sorted.Length>0 ? sorted[0].Value : 0f,
                weight1 = sorted.Length>1 ? sorted[1].Value : 0f,
                weight2 = sorted.Length>2 ? sorted[2].Value : 0f,
                weight3 = sorted.Length>3 ? sorted[3].Value : 0f
            };
        }

        public static string GetBlendShapeNameSafe(this Mesh mesh, string name)
        {
            if (mesh.GetBlendShapeIndex(name) == -1) return name;
            for (int i = 0; i < 999; i++)
            {
                string n = name + "_" + i;
                if (mesh.GetBlendShapeIndex(n) == -1) return n;
            }

            return null;
        }

        public static Transform[] GetHumanBones(this Animator human)
        {
            if (human.isHuman)
            {
                var bones = new List<Transform>();
                foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if(bone == HumanBodyBones.LastBone) continue;
                    bones.Add(human.GetBoneTransform(bone));
                }

                return bones.ToArray();
            }

            return null;
        }
    }

    public class BlendShapeData
    {
        public int offset = 0;
        public string name = "";
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector3> tangents = new List<Vector3>();
        
        public float bakedWeight = 0f;

        public BlendShapeData(Mesh origin,int index=0, int offset = 0,float weight = 0f)
        {
            Vector3[] blendShapeVertices = new Vector3[origin.vertexCount];
            Vector3[] blendShapeNormals = new Vector3[origin.vertexCount];
            Vector3[] blendShapeTangents = new Vector3[origin.vertexCount];
            origin.GetBlendShapeFrameVertices(index, 0, blendShapeVertices, blendShapeNormals, blendShapeTangents);
            this.offset = offset;
            this.name = origin.GetBlendShapeName(index);
            this.vertices = blendShapeVertices.ToList();
            this.normals = blendShapeNormals.ToList();
            this.tangents = blendShapeTangents.ToList();
            this.bakedWeight = weight;
        }
        public BlendShapeData(Mesh origin,
            string name,
            List<Vector3> destVertices,
            List<Vector3> destNormals,
            List<Vector4> destTangents)
        {
            if (origin.vertexCount == destVertices.Count &&
                origin.vertexCount == destNormals.Count &&
                origin.vertexCount == destTangents.Count)
            {
                var blendShapeVertices = new List<Vector3>();
                var blendShapeNormals = new List<Vector3>();
                var blendShapeTangents = new List<Vector3>();
                for (int i = 0; i < origin.vertexCount; i++)
                {
                    blendShapeVertices.Add(destVertices[i]-origin.vertices[i]);
                    blendShapeNormals.Add(destNormals[i]-origin.normals[i]);
                    blendShapeTangents.Add(destTangents[i]-origin.tangents[i]);
                }
                this.vertices = blendShapeVertices;
                this.normals = blendShapeNormals;
                this.tangents = blendShapeTangents;
                this.name = name;
            }
        }
        
        public BlendShapeData(
            string name,
            List<Vector3> srcVertices,
            List<Vector3> destVertices,
            Mesh mesh = null)
        {
            if (mesh == null)
            {
                if (srcVertices.Count == destVertices.Count)
                {
                    var blendShapeVertices = new List<Vector3>();
                    var blendShapeNormals = new List<Vector3>();
                    var blendShapeTangents = new List<Vector3>();
                    for (int i = 0; i < srcVertices.Count; i++)
                    {
                        blendShapeVertices.Add(destVertices[i]-srcVertices[i]);
                        blendShapeNormals.Add(Vector3.zero);
                        blendShapeTangents.Add(Vector3.zero);
                    }
                    this.vertices = blendShapeVertices;
                    this.normals = blendShapeNormals;
                    this.tangents = blendShapeTangents;
                    this.name = name;
                }
            }
            else
            {
                mesh.SetVertices(srcVertices);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                List<Vector3> srcNormals = new List<Vector3>();
                List<Vector4> srcTangents = new List<Vector4>();
                mesh.GetNormals(srcNormals);
                mesh.GetTangents(srcTangents);
                
                mesh.SetVertices(destVertices);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                List<Vector3> destNormals = new List<Vector3>();
                List<Vector4> destTangents = new List<Vector4>();
                mesh.GetNormals(destNormals);
                mesh.GetTangents(destTangents);
                
                if (srcVertices.Count == destVertices.Count)
                {
                    var blendShapeVertices = new List<Vector3>();
                    var blendShapeNormals = new List<Vector3>();
                    var blendShapeTangents = new List<Vector3>();
                    for (int i = 0; i < srcVertices.Count; i++)
                    {
                        blendShapeVertices.Add(destVertices[i] - srcVertices[i]);
                        blendShapeNormals.Add((destNormals[i] - srcNormals[i])*100f);
                        blendShapeTangents.Add((destTangents[i] - srcTangents[i])*100f);
                    }
                    this.vertices = blendShapeVertices;
                    this.normals = blendShapeNormals;
                    this.tangents = blendShapeTangents;
                    this.name = name;
                }
            }
        }

        public BlendShapeData(BlendShapeData origin)
        {
            this.name = origin.name;
            this.offset = origin.offset;
            this.vertices = origin.vertices.ToList();
            this.normals = origin.normals.ToList();
            this.tangents = origin.tangents.ToList();
            this.bakedWeight = origin.bakedWeight;
        }

        public BlendShapeData Clone()
        {
            return  new BlendShapeData(this);
        }
        
        public BlendShapeData AddOffset(int i)
        {
            this.offset += i;
            return this;
        }

        public BlendShapeData TransformRoot(Transform from)
        {
            var mat = from.worldToLocalMatrix;
            TransformRoot(mat);
            return this;
        }

        public BlendShapeData TransformRoot(Transform from,Transform to)
        {
            /*var mat = to.worldToLocalMatrix * from.localToWorldMatrix;
            Debug.Log(mat.lossyScale);
            mat = Matrix4x4.TRS(Vector3.zero, mat.inverse.rotation, new Vector3(
                mat.lossyScale.x*mat.lossyScale.x*mat.lossyScale.x,
                mat.lossyScale.y*mat.lossyScale.y*mat.lossyScale.y,
                mat.lossyScale.z*mat.lossyScale.z*mat.lossyScale.z));*/
            var mat = to.worldToLocalMatrix * from.localToWorldMatrix;
            TransformRoot(mat); 
            return this;
        }
        
        public BlendShapeData TransformRoot(Matrix4x4 mat)
        {
            //var mat = Matrix4x4.TRS(Vector3.zero, trans.rotation, trans.lossyScale);
            this.vertices = vertices.Select(v=>  mat.MultiplyVector(v)).ToList();
            this.normals = normals.Select(n=> mat.MultiplyVector(n)).ToList();
            this.tangents = tangents.Select(t=> mat.MultiplyVector(t)).ToList();
            return this;
        }

        public BlendShapeData RemoveVertexIndex(int index)
        {
            if (offset < index && index < offset + vertices.Count)
            {
                vertices.RemoveAt(index-offset);
                normals.RemoveAt(index-offset);
                tangents.RemoveAt(index-offset);
            }

            return this;
        }

        public BlendShapeData AddVertexIndex(int index,int[] origin,float[] weight = null)
        {
            if(weight==null) weight = new float[0];
            if (weight.Length != origin.Length)
                weight = Enumerable.
                    Range(0, origin.Length).
                    Select(_ => 1f / origin.Length).ToArray();
            var v = Vector3.zero;
            var n = Vector3.zero;
            var t = Vector3.zero;
            for (int i = 0; i < origin.Length; i++)
            {
                if (0 < origin[i] - offset && origin[i] - offset < vertices.Count)
                {
                    v += vertices[origin[i]-offset] * weight[i];
                    n += normals[origin[i]-offset] * weight[i];
                    t += tangents[origin[i]-offset] * weight[i];
                }
            }
            AddVertexIndex(index,v,n,t);

            return this;
        }
        
        public BlendShapeData AddVertexIndex(int index,int origin)
        {
            if (offset < origin && origin < offset + vertices.Count)
            {
                AddVertexIndex(index,vertices[origin-offset],normals[origin-offset],tangents[origin-offset]);
            }
            else
            {
                AddVertexIndex(index,Vector3.zero,Vector3.zero,Vector3.zero);
            }

            return this;
        }

        public BlendShapeData AddVertexIndex(int index,Vector3 v,Vector3 n,Vector3 t)
        {
            if (offset <= index && index < offset + vertices.Count)
            {
            }
            else
            if(index < offset)
            {
                var vs = Enumerable.Range(0, offset - index).Select(_ => Vector3.zero).ToList();
                var ns = Enumerable.Range(0, offset - index).Select(_ => Vector3.zero).ToList();
                var ts = Enumerable.Range(0, offset - index).Select(_ => Vector3.zero).ToList();
                vs.AddRange(vertices);
                ns.AddRange(normals);
                ts.AddRange(tangents);
                vertices = vs;
                normals = ns;
                tangents = ts;
                offset = index;
            }
            else
            if( offset + vertices.Count <= index)
            {
                var vs = Enumerable.Range(0, index - (offset + vertices.Count)+1).Select(_ => Vector3.zero).ToList();
                var ns = Enumerable.Range(0, index - (offset + vertices.Count)+1).Select(_ => Vector3.zero).ToList();
                var ts = Enumerable.Range(0, index - (offset + vertices.Count)+1).Select(_ => Vector3.zero).ToList();
                vertices.AddRange(vs);
                normals.AddRange(ns);
                tangents.AddRange(ts);
            }

            vertices[index - offset] = v;
            normals[index - offset] = n;
            tangents[index - offset] = t;

            return this;
        }
        
        public BlendShapeData ApplyTable(List<int> table)
        {
            int removed = 0;
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i] < offset)
                {
                    offset--;
                }
                else
                if(table[i]-offset-removed < vertices.Count)
                {
                    vertices.RemoveAt(table[i]-offset-removed);
                    normals.RemoveAt(table[i]-offset-removed);
                    tangents.RemoveAt(table[i]-offset-removed);
                    removed++;
                }
            }
            return this;
        }

        public void Apply(ref Mesh clone)
        {
            List<Vector3> blendShapeVertices = new List<Vector3>();
            List<Vector3> blendShapeNormals = new List<Vector3>();
            List<Vector3> blendShapeTangents = new List<Vector3>();
            for (int i = 0; i < clone.vertexCount; ++i)
            {
                if (i >= offset && i < offset + vertices.Count)
                {
                    blendShapeVertices.Add(vertices[i - offset]);
                    blendShapeNormals.Add(normals[i - offset]*0.01f);
                    blendShapeTangents.Add(tangents[i - offset]*0.01f);
                }
                else
                {
                    blendShapeVertices.Add(Vector3.zero);
                    blendShapeNormals.Add(Vector3.zero);
                    blendShapeTangents.Add(Vector3.zero);
                }
            }
            
            if(bakedWeight != 0f) ApplyBlendShape(ref clone,-bakedWeight);

            clone.AddBlendShapeFrame( clone.GetBlendShapeNameSafe(name), 100, blendShapeVertices.ToArray(), blendShapeNormals.ToArray(), blendShapeTangents.ToArray());
        }
        
        public void ApplyBlendShape(ref Mesh clone,float weight = -100f)
        {
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var ts = new List<Vector4>();
            clone.GetVertices(vs);
            clone.GetNormals(ns);
            clone.GetTangents(ts);
            ApplyBlendShape(ref vs,ref ns,ref ts,weight);
            clone.SetVertices(vs);
            clone.SetNormals(ns);
            clone.SetTangents(ts);
        }
        
        public void ApplyBlendShape(ref List<Vector3> vs,ref List<Vector3> ns,ref List<Vector4> ts,float weight = -100f)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vs[offset + i] = vs[offset + i] + vertices[i] * (weight / 100f);
            }
        }
    }
}