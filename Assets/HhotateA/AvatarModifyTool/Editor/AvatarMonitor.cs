/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using UnityEngine;
using UnityEditor;

namespace HhotateA.AvatarModifyTools.Core
{
    /// <summary>
    /// 編集画面のカメラ画像出すための補助クラス
    /// </summary>
    public class AvatarMonitor
    {
        private GameObject baseObject;
        private Camera camera;
        private RenderTexture targetTexture;
        private Rect rect;

        private const int previewLayer = 2;

        private float bound = 1f;
        public float GetBound => bound;

        public AvatarMonitor(Transform root)
        {
            baseObject = new GameObject("CameraRoot");
            baseObject.transform.SetParent(root);
            baseObject.transform.localPosition = Vector3.zero;
            ;
            baseObject.transform.localRotation = Quaternion.identity;
            baseObject.hideFlags = HideFlags.HideAndDontSave;

            var c = new GameObject("Camera");
            c.transform.SetParent(baseObject.transform);
            c.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            ;
            c.transform.localRotation = Quaternion.identity;
            c.hideFlags = HideFlags.HideAndDontSave;

            camera = c.AddComponent<Camera>();
            targetTexture = new RenderTexture(1000, 1000, 1);
            camera.targetTexture = targetTexture;

            var bounds = GetMaxBounds(root.gameObject);
            bound = Mathf.Max(bounds.extents.x , bounds.extents.y , bounds.extents.z);
            camera.nearClipPlane = bound*0.01f;
            camera.farClipPlane = bound*10f;
        }
        
        Bounds GetMaxBounds(GameObject g) {
            var renderers = g.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (Renderer r in renderers) {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        ~AvatarMonitor()
        {
            Release();
        }

        public Vector3 WorldSpaceCameraVec()
        {
            return camera.transform.forward;
        }
        
        public Vector3 WorldSpaceCameraUp()
        {
            return camera.transform.up;
        }

        public void Release()
        {
            GameObject.DestroyImmediate(baseObject.gameObject);
            targetTexture.Release();
        }

        public void ResizeTexture(int width, int height)
        {
            targetTexture = new RenderTexture(width, height, 1);
            camera.targetTexture = targetTexture;
        }

        public Texture GetTexture()
        {
            return targetTexture as Texture;
            ;
        }

        public void Display(int width, int height, int rotationDrag = 1, int positionDrag = 2)
        {
            if (width != targetTexture.width || height != targetTexture.height)
            {
                ResizeTexture(width, height);
            }

            rect = GUILayoutUtility.GetRect(targetTexture.width, targetTexture.height, GUI.skin.box);
            EditorGUI.DrawPreviewTexture(rect, GetTexture());

            var e = Event.current;

            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag && e.button == rotationDrag)
                {
                    // Drag
                    var r = baseObject.transform.rotation;
                    baseObject.transform.RotateAround(baseObject.transform.position, Vector3.up, e.delta.x * 0.1f);
                    baseObject.transform.RotateAround(baseObject.transform.position, baseObject.transform.right,
                        e.delta.y * 0.1f);
                }

                if (e.type == EventType.MouseDrag && e.button == positionDrag)
                {
                    // Drag
                    var r = baseObject.transform.rotation;
                    baseObject.transform.position =
                        baseObject.transform.position + camera.transform.up * e.delta.y * 0.001f * bound;
                    baseObject.transform.position =
                        baseObject.transform.position + camera.transform.right * -e.delta.x * 0.001f * bound;
                }


                if (e.type == EventType.ScrollWheel)
                {
                    baseObject.transform.position =
                        baseObject.transform.position + camera.transform.forward * -e.delta.y * 0.01f * bound;
                }

                /*
                if (e.keyCode == KeyCode.W)
                {
                    baseObject.transform.position = baseObject.transform.position + camera.transform.forward * Time.deltaTime*0.1f;
                }
                */
            }

            camera.Render();
        }

        public bool IsInDisplay(Vector2 pos)
        {
            return rect.Contains(pos);
        }

        public void GetControllPoint(MeshCollider meshCollider, bool isSelectVertex, Action<Vector3> onHit)
        {
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                // Drag
                var p = new Vector3(e.mousePosition.x - rect.x, targetTexture.height - e.mousePosition.y + rect.y,
                    1f);
                if (isSelectVertex)
                {
                    GetVertexPosition(meshCollider, camera.ScreenPointToRay(p), h => { onHit?.Invoke(h); });
                }
                else
                {
                    GetHit(meshCollider, camera.ScreenPointToRay(p), h => { onHit?.Invoke(h); });
                }
            }
        }

        public void GetHit(MeshCollider meshCollider, Ray ray, Action<Vector3> onHit = null)
        {
            var hits = Physics.RaycastAll(ray, bound, 1 << previewLayer);

            foreach (var hit in hits)
            {
                MeshCollider mc = hit.collider as MeshCollider;

                if (mc == meshCollider)
                {
                    onHit?.Invoke(meshCollider.transform.InverseTransformPoint(hit.point));
                    return;
                }
            }
        }

        public void GetTriangle(MeshCollider meshCollider, Action<int> onhit = null)
        {
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                var p = new Vector3(e.mousePosition.x - rect.x, targetTexture.height - e.mousePosition.y + rect.y,
                    1f);
                var ray = camera.ScreenPointToRay(p);
                var hits = Physics.RaycastAll(ray, bound, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;

                    if (mc == meshCollider)
                    {
                        onhit?.Invoke(hit.triangleIndex);
                        return;
                    }
                }
            }
        }
        
        public void GetTriangle(MeshCollider meshCollider, Action<int,Vector3> onhit = null)
        {
            var e = Event.current;
            
            if (rect.Contains(e.mousePosition))
            {
                var p = new Vector3(e.mousePosition.x - rect.x, targetTexture.height - e.mousePosition.y + rect.y,
                    1f);
                var ray = camera.ScreenPointToRay(p);
                var hits = Physics.RaycastAll(ray, bound, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;

                    if (mc == meshCollider)
                    {
                        onhit?.Invoke(hit.triangleIndex,meshCollider.transform.InverseTransformPoint(hit.point));
                        return;
                    }
                }
            }
        }
        
        public void GetDragTriangle(MeshCollider meshCollider, Action<int,Vector3,int,Vector3> onhit = null)
        {
            var e = Event.current;
            
            if (rect.Contains(e.mousePosition))
            {
                var p = new Vector3(e.mousePosition.x - rect.x, rect.height - e.mousePosition.y + rect.y,1f);
                var ray = camera.ScreenPointToRay(p);
                var hits = Physics.RaycastAll(ray, bound, 1 << previewLayer);
                
                // drag前
                var pd = new Vector3(e.mousePosition.x - rect.x - e.delta.x, rect.height - e.mousePosition.y + rect.y + e.delta.y,1f);
                var rayd = camera.ScreenPointToRay(pd);
                var hitsd = Physics.RaycastAll(rayd, bound, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;
                    if (mc == meshCollider)
                    {
                        foreach (var hitd in hitsd)
                        {
                            MeshCollider mcd = hitd.collider as MeshCollider;

                            if (mcd == meshCollider)
                            {
                                onhit?.Invoke(hitd.triangleIndex,
                                    meshCollider.transform.InverseTransformPoint(hitd.point),
                                    hit.triangleIndex,
                                    meshCollider.transform.InverseTransformPoint(hit.point));
                                return;
                            }
                        }
                        return;
                    }
                }
            
            }
        }
        
        public void GetVertex(MeshCollider meshCollider, Action<int,Vector3> onHit)
        {
            var e = Event.current;
            
            if (rect.Contains(e.mousePosition))
            {
                // Drag
                var p = new Vector3(e.mousePosition.x - rect.x, targetTexture.height - e.mousePosition.y + rect.y,1f);
                
                var hits = Physics.RaycastAll(camera.ScreenPointToRay(p), bound, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;

                    if (mc == meshCollider)
                    {
                        Mesh mesh = meshCollider.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        int[] triangles = mesh.triangles;
                        int[] indexs = new int[3] {triangles[hit.triangleIndex * 3 + 0],triangles[hit.triangleIndex * 3 + 1],triangles[hit.triangleIndex * 3 + 2]};

                        int i = GetNearIndex(
                            meshCollider.transform.InverseTransformPoint(hit.point),
                            new Vector3[] {vertices[indexs[0]], vertices[indexs[1]], vertices[indexs[2]]});

                        int index = indexs[i];

                        onHit?.Invoke(index,vertices[index]);

                        return;
                    }
                }
            }
        }

        public void GetVertexPosition(MeshCollider meshCollider, Ray ray, Action<Vector3> onhit = null)
        {
            var hits = Physics.RaycastAll(ray, bound, 1 << previewLayer);

            foreach (var hit in hits)
            {
                MeshCollider mc = hit.collider as MeshCollider;

                if (mc == meshCollider)
                {
                    Mesh mesh = meshCollider.sharedMesh;
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
                    Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
                    Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];

                    onhit?.Invoke(GetNearPoint(
                        meshCollider.transform.InverseTransformPoint(hit.point),
                        new Vector3[] {p0, p1, p2}));

                    return;
                }
            }
        }

        Vector3 GetNearPoint(Vector3 origin, Vector3[] points)
        {
            float near = 1000f;
            Vector3 output = Vector3.zero;
            foreach (var point in points)
            {
                var d = Vector3.Distance(origin, point);
                if (d < near)
                {
                    output = point;
                    near = d;
                }
            }

            return output;
        }
        
        int GetNearIndex(Vector3 origin, Vector3[] points)
        {
            float near = 1000f;
            int output = -1;
            for (int i = 0; i < points.Length; i++)
            {
                var d = Vector3.Distance(origin, points[i]);
                if (d < near)
                {
                    output = i;
                    near = d;
                }
            }

            return output;
        }
    }
}
