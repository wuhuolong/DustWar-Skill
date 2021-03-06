﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class SkillController : MonoBehaviour
{
    bool isSkill = false;

    public GameObject SkillCastObject;
    List<Transform> Trigers = new List<Transform>(); 
    List<GameObject> EnemysInSkill = new List<GameObject>();
    public SkillAreaType SkillType;
    private SkillArea SkillAreaParam;

    public float outerCircleRadius = 12f;
    Transform innerCircleTrans;
    //Vector2 outerCircleStartWorldPos = Vector2.zero;

    //委托定义
    public Action<Vector2> showSkill;
    public Action hideSkill;
    public Action<Vector2> moveSkill;
    void Awake()
    {
        innerCircleTrans = transform.GetChild(0);
        Trigers.Add (transform.GetChild(1));
        Trigers.Add  (transform.GetChild(2));
    }

    void Start()
    {
        Trigers[0].gameObject.SetActive(false);
        Trigers[1].gameObject.SetActive(false);
        SkillAreaParam=GetComponent<SkillArea>();
    }
    void Update()
    {
        //检测输入，变更isSkill
        SkillInputCheck();
        if (isSkill)
        {
            int layerMask = 1 << 8;
            layerMask = ~layerMask;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit,1000f,layerMask);
            Vector3 TemptouchPos =new Vector3(hit.point.x - transform.position.x,0,hit.point.z-transform.position.z);
            Vector2 touchPos = new Vector2(TemptouchPos.x, TemptouchPos.z);
            if (showSkill != null)
                showSkill(innerCircleTrans.localPosition);
            if (touchPos.magnitude < outerCircleRadius)
               innerCircleTrans.localPosition = touchPos;
            else
                innerCircleTrans.localPosition = touchPos.normalized * outerCircleRadius;

            if (moveSkill != null)
                moveSkill(innerCircleTrans.localPosition);

            //技能检测前触开始发器设置
            SkillCheckStart(SkillType);
        }
        else
        {
            if (isSkill)
            {
                isSkill = false;
                innerCircleTrans.localPosition = Vector3.zero;
            }

            if (hideSkill != null)
                hideSkill();
        }
    }
   /// <summary>
   ///技能开始检测，根据技能类型设置触发器大小和位置
   /// </summary>
   /// <param name="checkMode">技能类型</param>
    void SkillCheckStart(SkillAreaType checkMode)
    {
        switch (checkMode)
        {
            case SkillAreaType.OuterCircle:
                Trigers[0].gameObject.SetActive(true);
                Trigers[0].position = transform.position;
                Trigers[0].localScale=new Vector3(SkillAreaParam.outerRadius,0,SkillAreaParam.outerRadius);
                break;
            case SkillAreaType.OuterCircle_InnerCircle:
                Trigers[0].gameObject.SetActive(true);
                Trigers[0].localScale = new Vector3(SkillAreaParam.innerRadius, 0, SkillAreaParam.innerRadius);
                Trigers[0].position=SkillAreaParam.deltaVec+transform.position;
                break;
            case SkillAreaType.OuterCircle_InnerSector:
                Trigers[0].gameObject.SetActive(true);
                Trigers[0].position = transform.position;
                Trigers[0].localScale = new Vector3(SkillAreaParam.outerRadius, 0, SkillAreaParam.outerRadius);
                break;
            case SkillAreaType.OuterCircle_InnerCube:
                Trigers[1].gameObject.SetActive(true);
                BoxCollider boxCol = Trigers[1].GetComponent<BoxCollider>();
                boxCol.size = new Vector3(1, 1, SkillAreaParam.outerRadius/2);
                boxCol.center = new Vector3(0, 0, SkillAreaParam.outerRadius/4);
                Trigers[0].localScale = new Vector3(SkillAreaParam.cubeWidth, 1, 1);
                Trigers[1].LookAt(SkillAreaParam.deltaVec+transform.position,transform.up);
                break;

        }
    }
  
    //在列表的中的敌人触发效果，列表清空，触发器设置为False
    void SkillCheckTrigger()
    {
        foreach (GameObject enemy in EnemysInSkill)
        {
            Debug.Log("技能击中了"+enemy.name);
        }
        if (SkillType == SkillAreaType.OuterCircle_InnerCubeCast)
        {
            GameObject obj= Instantiate(SkillCastObject, transform.position, Quaternion.LookRotation(SkillAreaParam.deltaVec));
            //obj.transform.LookAt(SkillAreaParam.deltaVec);
        }
        Trigers[0].gameObject.SetActive(false);
        Trigers[1].gameObject.SetActive(false);
        EnemysInSkill.Clear();

    }
    void OnTriggerEnter(Collider col)
    {
        //进入碰撞器的为敌人则加入列表
        if (col.tag == "Enemy")
        {
            EnemysInSkill.Add(col.gameObject);
        }
    }    
    void OnTriggerStay(Collider col)
    {
        //判断停留在碰撞器中的敌人是否符合检测条件
        Vector3 enemyPos = col.gameObject.transform.position;
        switch (SkillType)
        {
            case SkillAreaType.OuterCircle:
                if (Vector3.Distance(enemyPos, Trigers[0].position) > outerCircleRadius)
                {
                    EnemysInSkill.Remove(col.gameObject);
                }
                break;
            case SkillAreaType.OuterCircle_InnerCircle:
                if (Vector3.Distance(enemyPos, Trigers[0].position) > outerCircleRadius)
                {
                    EnemysInSkill.Remove(col.gameObject);
                }
                break;
            case SkillAreaType.OuterCircle_InnerSector:
                if (Vector3.Distance(enemyPos, Trigers[0].position) > outerCircleRadius || Vector3.Angle(SkillAreaParam.deltaVec, enemyPos - Trigers[0].position) > SkillAreaParam.angle * 0.5)
                {
                    EnemysInSkill.Remove(col.gameObject);
                }
                else if (!EnemysInSkill.Contains(col.gameObject) && col.tag == "Enemy")
                {
                    EnemysInSkill.Add(col.gameObject);
                }
                break;
            case SkillAreaType.OuterCircle_InnerCube:
                break;
        }
    }
    void OnTriggerExit(Collider col)
    {
        //退出碰撞器的移出列表
        EnemysInSkill.Remove(col.gameObject);
    }
    /// <summary>
    /// 输入检测并执行部分检测后的操作
    /// </summary>
    void SkillInputCheck()
    { 
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            isSkill = !isSkill;
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0) && isSkill)
        {
            isSkill = !isSkill;
            //触发检测
            SkillCheckTrigger();
        }
    }
}
