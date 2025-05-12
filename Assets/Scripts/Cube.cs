using UnityEngine;
using DG.Tweening;

public class Cube : MonoBehaviour
{
    public bool IsFilled  = false;
    public bool CanHarm = false;

    public void ResetCube()
    {   
        IsFilled = false;
        CanHarm  = false;
        transform.localPosition = Vector3.zero;
        gameObject.SetActive(false);
    }  

    public void Initalize(Vector3 pos, bool isFilled = false)
    {
        transform.position = pos;
        IsFilled = isFilled;
        gameObject.SetActive(true);
    }

    public void FillCube()
    {
        if(!gameObject.activeSelf || IsFilled)
            return;
        IsFilled = true;
        GridManager.Instance.ChangeValue(transform.position.x, transform.position.z);
        transform.DOMoveY((transform.position.y + 0.5f), 0.15f);
    }
}