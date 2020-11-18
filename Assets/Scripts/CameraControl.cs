using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
 * 类、方法名、public变量、常量、枚举名和枚举值 所有单词开头字母大写(Pascal规则)
 * protected/private变量、方法参数 都是开头字母小写，第二个单词开始大写(camel规则)
 */

/* 
 * 所有的UI操作都要屏蔽相机操作
 * 尽量少在update中操作
 * 点选物体和相机旋转操作冲突(待办)
 * 全屏幕透明Image，使用UI的鼠标拖拽IDragHandler，滚轮接口实现类似效果，鼠标操作在接口函数实现，可以把update空出来
 */

// 相机模式
public enum CameraType
{
    Free,
    Focus
}

// 聚焦模式下取消选中物体的方式
public enum DoType
{
    Mouse,
    Button
}

public class CameraControl : MonoBehaviour {

    // 控制平移速度
    public float MoveSpeed = 2.0f;
    // 控制旋转速度
    public float RotateSpeed = 2.0f;
    // 当前相机模式
    public CameraType NowType = CameraType.Free;
    // 聚焦模式下聚焦中心的物体
    public GameObject FocusObject=null;
    // 聚焦模式下如何取消选中
    public DoType NowDoType = DoType.Mouse;

    // 当前操纵的相机
    private Camera mainCamera = null;
    // 记录自由模式当前的位置和方向
    private Vector3 nowPos, nowLookAt;

    private void Start()
    {
        mainCamera = Camera.main;

        // 记录当前相机位置和方向
        nowPos = mainCamera.transform.position;
        nowLookAt = mainCamera.transform.forward + mainCamera.transform.position;

        if (NowType == CameraType.Focus)
        {
            SetFocusObject(FocusObject, new Vector3(-3, 3, -3));
        }
    }

    void Update ()
    {
        // 点击UI时不触发射线
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 获取鼠标输入
        float _mouseX = Input.GetAxis("Mouse X");
        float _mouseY = Input.GetAxis("Mouse Y");

        // 没有聚焦物体，自由模式
        if (FocusObject==null)
        {
            // 记录当前相机位置和方向
            nowPos = mainCamera.transform.position;
            nowLookAt = mainCamera.transform.forward + mainCamera.transform.position;

            if (NowDoType == DoType.Mouse)
            {
                // 点击选择物体并进入聚焦模式
                if (Input.GetMouseButtonDown(0))
                {
                    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);//鼠标的屏幕坐标转化为一条射线
                    RaycastHit hit;

                    //屏蔽mask层
                    int layerMask = 1 << 8;
                    layerMask = ~layerMask;
                    // 射线距离1000
                    if (Physics.Raycast(ray, out hit, 1000, layerMask))
                    {
                        var g = hit.collider.gameObject;
                        SetFocusObject(g, new Vector3(-3, 3, -3));
                    }
                }
            }

            CameraRotate(_mouseX, _mouseY);
            CameraMove();
        }
        else// 聚焦模式
        {
            // 如果取消模式是鼠标
            if (NowDoType == DoType.Mouse)
            {
                // 点击切换聚焦物体或取消聚焦模式
                if (Input.GetMouseButtonDown(0))
                {
                    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);//鼠标的屏幕坐标转化为一条射线
                    RaycastHit hit;

                    // 射线距离1000
                    if (Physics.Raycast(ray, out hit,1000))
                    {

                    }
                    else
                    {

                    }
                }
            }

            CameraRotateFocus(_mouseX, _mouseY);
            CameraMoveFocus(_mouseX, _mouseY);
        }

        // 滚轮缩放
        CameraScale();
    }

    // 设置聚焦物体并切换模式，由UI调用
    public void SetFocusObject(GameObject g, Vector3 pos)
    {
        GameObject pre = FocusObject;
        if(g!=null)// 物体存在，聚焦模式
        {
            FocusObject = g;
            mainCamera.transform.position = FocusObject.transform.position + pos;
            mainCamera.transform.LookAt(FocusObject.transform);
        }
        else// 物体不存在，取消聚焦模式(自由模式)
        {
            FocusObject = null;
            mainCamera.transform.position = nowPos;
            mainCamera.transform.LookAt(nowLookAt);
        }
    }

    // 获取当前的聚焦物体
    public GameObject GetFocusObject()
    {
        return FocusObject;
    }

    // 自由模式下相机旋转
    private void CameraRotate(float _mouseX, float _mouseY)
    {
        mainCamera.transform.Rotate(Vector3.up, _mouseX * RotateSpeed, Space.World);
        mainCamera.transform.Rotate(transform.right, -_mouseY * RotateSpeed, Space.World);
    }

    // 自由模式下相机平移
    private void CameraMove()
    {
        float value = 0;
        //上下移动
        if (Input.GetKey(KeyCode.Z))
            value = 1;
        else if (Input.GetKey(KeyCode.X))
            value = -1;
        //前后左右移动
        Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), value, Input.GetAxis("Vertical"));
        mainCamera.transform.Translate(moveDirection * Time.deltaTime * MoveSpeed, Space.Self);
    }

    // 聚焦模式下相机旋转
    private void CameraRotateFocus(float _mouseX, float _mouseY)
    {
        if (Input.GetMouseButton(0))
        {
            mainCamera.transform.RotateAround(FocusObject.transform.position, Vector3.up, _mouseX * RotateSpeed);
            mainCamera.transform.RotateAround(FocusObject.transform.position, mainCamera.transform.right, -_mouseY * RotateSpeed);
        }
    }

    // 聚焦模式下相机平移
    private void CameraMoveFocus(float _mouseX, float _mouseY)
    {
        if (Input.GetMouseButton(2))
        {
            Vector3 moveDir = (_mouseX * -mainCamera.transform.right + _mouseY * -mainCamera.transform.up);
            mainCamera.transform.position += moveDir * MoveSpeed *0.25f;
        }
    }

    // 滚轮缩放
    private void CameraScale()
    {
        //放大
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
             mainCamera.transform.position -= 0.5f* mainCamera.transform.forward;
        }
        //缩小
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            mainCamera.transform.position += 0.5f * mainCamera.transform.forward;
        }
    }
}
