using System;
using System.Collections.Generic;
using UnityEngine;

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

    public void SetSlerp(float slerp)
    {
        if (bone == null) return;

        Vector3 pos = bone.localPosition;
        Vector3 rotation = bone.localEulerAngles;
        Vector3 scale = bone.localScale;

        float value = Mathf.Lerp(startValue,endValue,slerp);
        pos.x = value;

        bone.localPosition = pos;
        bone.localRotation = Quaternion.Euler(rotation);
        bone.localScale = scale;
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
}
#endregion

public class FacePincher : MonoBehaviour {

    public List<FaceGroup> faceGroups = new List<FaceGroup>();

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
