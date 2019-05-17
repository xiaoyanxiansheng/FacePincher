using System;
using System.Collections.Generic;
using UnityEngine;

#region 骨骼信息的改变
public enum FaceBonesChangeType
{
    // 位置
    PositionX,
    PositionY,
    PositionZ,

    // 旋转
    RotationX,
    RotationY,
    RotationZ,

    // 缩放
    ScaleX,
    ScaleY,
    ScaleZ,
}

[Serializable]
public class FaceBone
{
    public string name = "";
    // 骨骼改变类型(坐标、旋转、缩放)
    public FaceBonesChangeType changeType = FaceBonesChangeType.PositionX;
    public Transform bone;          // 当前需要改变的骨骼
    public float startValue = 0;    // 差值的开始值
    public float endValue = 0;      // 差值的结束值       

    private Transform _preBone;     // 上一次改变的骨头(用于更新)
    // 上一次骨骼改变类型(用于更新)
    private FaceBonesChangeType _preChangeType = FaceBonesChangeType.PositionX;
    // 是否已经初始化
    private bool _isInit = false;

    // 是否需要重置
    private bool IsDirty()
    {
        return _preChangeType != changeType || _preBone != bone;
    }

    // 初始化数据
    private void Init()
    {
        if (_isInit)
            return;
        _isInit = true;
        SetInitTransValue(bone);
    }

    private void UnInit()
    {
        _isInit = false;
    }

    // 设置差值(0-1)
    public void SetSlerp(float slerp)
    {
        if (bone == null) return;

        if (IsDirty()) UnInit();
        Init();

        _preBone = bone;
        _preChangeType = changeType;

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

    // 根据骨骼初始化数据(改变骨骼和改变骨骼类型时会触发)
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

    // 重置数据(初始化成其他脸型)
    public void ResetData(FaceBone initFaceBone, Transform bone, Transform preBone)
    {
        name = initFaceBone.name;
        this.bone = bone;
        _preBone = preBone;
        changeType = initFaceBone.changeType;
        _preChangeType = changeType;
        startValue = initFaceBone.startValue;
        endValue = initFaceBone.endValue;
        _isInit = true;
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

    public void ResetData(FaceGroupPart initFaceGroupPart)
    {
        name = initFaceGroupPart.name;
        slerp = initFaceGroupPart.slerp;
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

    public void ResetData(FaceGroup initFaceGroup)
    {
        name = initFaceGroup.name;
    }
}
#endregion

#region 脸部管理
public class FacePincher : MonoBehaviour
{
    // 可以初始化成其他脸型
    public Transform initFace;
    // 设置引用脸型
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

    /// <summary>
    /// 克隆其他脸型
    /// </summary>
    [ContextMenu("CloneFace")]
    public void CloneFace()
    {
        if (initFace == null)
        {
            Debug.Log("缺少初始化脸型");
            return;
        }

        faceGroups.Clear();
        FacePincher initFacePicher = initFace.GetComponent<FacePincher>();
        foreach (FaceGroup faceGroup in initFacePicher.faceGroups)
        {
            // 初始化faceGroup数据
            FaceGroup tempGroup = new FaceGroup();
            faceGroups.Add(tempGroup);
            tempGroup.ResetData(tempGroup);
            foreach (FaceGroupPart faceGroupPart in faceGroup.faceGroupParts)
            {
                // 初始化faceGroupPart数据
                FaceGroupPart tempFaceGroupPart = new FaceGroupPart();
                tempGroup.faceGroupParts.Add(tempFaceGroupPart);
                tempFaceGroupPart.ResetData(faceGroupPart);
                foreach (FaceBone faceBone in faceGroupPart.faceBones)
                {
                    // 初始化faceBone数据
                    FaceBone tempFaceBones = new FaceBone();
                    tempFaceGroupPart.faceBones.Add(tempFaceBones);
                    string path = "";
                    Transform tempTrans = faceBone.bone;
                    Transform root = faceBone.bone.root;
                    while (tempTrans != root)
                    {
                        path = tempTrans.name + path;
                        if (tempTrans.parent != root)
                            path = "/" + path;
                        tempTrans = tempTrans.parent;
                    }
                    Transform bone = transform.Find(path);
                    tempFaceBones.ResetData(faceBone, bone, bone);
                }
            }
        }
        OnValidate();
    }

    /// <summary>
    /// 设置引用脸型
    /// </summary>
    [ContextMenu("BakeMesh")]
    public void BakeMesh()
    {
        if (linkMeshRenderer == null) return;

        tempMesh.Clear(true);
        pincherMeshRenderer.BakeMesh(tempMesh);
        // 变化矩阵
        tempMesh.bindposes = linkMeshRenderer.sharedMesh.bindposes;
        // 骨骼权重
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
#endregion