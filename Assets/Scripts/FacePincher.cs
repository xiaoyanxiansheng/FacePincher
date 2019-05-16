using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#region 骨骼信息的改变
public enum FaceBonesChangeType
{
    PositionX,
    PositionY,
    PositionZ,

    RotationX,
    RotationY,
    RotationZ,

    ScaleX,
    ScaleY,
    ScaleZ,
}
[Serializable]
public class FaceBone
{
    public string name = "";
    public FaceBonesChangeType changeType = FaceBonesChangeType.PositionX;
    public Transform bone;
    public float startValue = 0;
    public float endValue = 0;
    [HideInInspector]
    public bool isInit = false;
    [HideInInspector]
    public Transform preBone;
    [HideInInspector]
    public FaceBonesChangeType preChangeType = FaceBonesChangeType.PositionX;

    private bool IsDirty()
    {
        return preChangeType != changeType || preBone != bone;
    }
    private void Init()
    {
        if (isInit)
            return;
        isInit = true;
        SetInitTransValue(bone);
    }
    private void UnInit()
    {
        isInit = false;
    }

    public void InitBone(Transform initFace)
    {
        if (bone == null) return;

        // 取当前骨骼路径然后去初始化脸中找初始化数据
        string path = "";
        Transform tempTrans = bone;
        Transform root = bone.root;
        while(tempTrans != root)
        {
            path = tempTrans.name + path;
            if (tempTrans.parent != root)
                path = "/" + path;
            tempTrans = tempTrans.parent;
        }
        Transform initTrans = initFace.Find(path);
        SetInitTransValue(initTrans);
    }

    public void SetSlerp(float slerp)
    {
        if (bone == null) return;

        if (IsDirty()) UnInit();
        Init();

        preBone = bone;
        preChangeType = changeType;

        Vector3 pos = bone.localPosition;
        Vector3 rotation = bone.localEulerAngles;
        Vector3 scale = bone.localScale;

        float value = Mathf.Lerp(startValue, endValue, slerp);
        switch (changeType)
        {
            case FaceBonesChangeType.PositionX:
                pos.x = value;
                break;
            case FaceBonesChangeType.PositionY:
                pos.y = value;
                break;
            case FaceBonesChangeType.PositionZ:
                pos.z = value;
                break;
            case FaceBonesChangeType.RotationX:
                rotation.x = value;
                break;
            case FaceBonesChangeType.RotationY:
                rotation.y = value;
                break;
            case FaceBonesChangeType.RotationZ:
                rotation.z = value;
                break;
            case FaceBonesChangeType.ScaleX:
                scale.x = value;
                break;
            case FaceBonesChangeType.ScaleY:
                scale.y = value;
                break;
            case FaceBonesChangeType.ScaleZ:
                scale.z = value;
                break;
        }

        bone.localPosition = pos;
        bone.localRotation = Quaternion.Euler(rotation);
        bone.localScale = scale;
    }

    public void SetInitTransValue(Transform sourceBone)
    {
        if (sourceBone == null)
            return;
        Vector3 pos = sourceBone.localPosition;
        Vector3 rotation = sourceBone.localEulerAngles;
        Vector3 scale = sourceBone.localScale;
        float value = 0;
        switch (changeType)
        {
            case FaceBonesChangeType.PositionX:
                value = pos.x;
                break;
            case FaceBonesChangeType.PositionY:
                value = pos.y;
                break;
            case FaceBonesChangeType.PositionZ:
                value = pos.z;
                break;
            case FaceBonesChangeType.RotationX:
                value = rotation.x;
                break;
            case FaceBonesChangeType.RotationY:
                value = rotation.y;
                break;
            case FaceBonesChangeType.RotationZ:
                value = rotation.z;
                break;
            case FaceBonesChangeType.ScaleX:
                value = scale.x;
                break;
            case FaceBonesChangeType.ScaleY:
                value = scale.y;
                break;
            case FaceBonesChangeType.ScaleZ:
                value = scale.z;
                break;
        }
        startValue = value;
        endValue = value;
    }
}
#endregion

#region 部位单一属性改变 可能需要同时修改多根骨骼的Transform信息
[Serializable]
public class FaceGroupPart
{
    public string name = "";
    public List<FaceBone> faceBones = new List<FaceBone>();
    [Range(0,1)]
    public float slerp = 0;

    public void Refresh()
    {
        foreach(FaceBone faceBone in faceBones)
        {
            faceBone.SetSlerp(slerp);
        }
    }

    public void InitGroupPart(Transform initFace)
    {
        foreach (FaceBone faceBone in faceBones)
        {
            faceBone.InitBone(initFace);
        }
    }
}
#endregion

#region 部位改变 可能要改变多种组合(位置，旋转，缩放)
[Serializable]
public class FaceGroup
{
    public string name = "";
    public List<FaceGroupPart> faceGroupParts = new List<FaceGroupPart>();

    public void Refresh()
    {
        foreach (FaceGroupPart faceGroupPart in faceGroupParts)
        {
            faceGroupPart.Refresh();
        }
    }

    public void InitGroup(Transform initFace)
    {
        foreach(FaceGroupPart faceGroupPart in faceGroupParts)
        {
            faceGroupPart.InitGroupPart(initFace);
        }
    }
}
#endregion

public class FacePincher : MonoBehaviour
{
    public Transform initFace;
    public SkinnedMeshRenderer linkMeshRenderer;
    [HideInInspector]
    public SkinnedMeshRenderer pincherMeshRenderer;
    public List<FaceGroup> faceGroups = new List<FaceGroup>();

    private Mesh tempMesh;

    private void Awake()
    {
        pincherMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        tempMesh = new Mesh();
    }

    [ContextMenu("InitFace")]
    public void InitFace()
    {
        if (initFace == null)
        {
            Debug.Log("缺少初始化脸型");
            return;
        }

        foreach(FaceGroup faceGroup in faceGroups)
        {
            faceGroup.InitGroup(initFace);
        }
    }
    [ContextMenu("BakeMesh")]
    public void BakeMesh()
    {
        if (linkMeshRenderer == null) return;

        tempMesh.Clear(true);
        pincherMeshRenderer.BakeMesh(tempMesh);
        tempMesh.bindposes = linkMeshRenderer.sharedMesh.bindposes;
        tempMesh.boneWeights = linkMeshRenderer.sharedMesh.boneWeights;
        linkMeshRenderer.sharedMesh = tempMesh;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }
        foreach(FaceGroup faceGroup in faceGroups)
        {
            faceGroup.Refresh();
        }
    }
#endif
}
