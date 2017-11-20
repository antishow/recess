﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;

public class HandController : NetworkBehaviour
{
    public static float interactSphereRadius = 1.0f;
    private const int INTERACTIVE = 1 << 8;
    public HoldableItem HeldObject;
    public float ThrowSpeed;
    private float ChargeLevel;
    private float ChargeMax = 100.0f;
    private NetworkIdentity networkIdentity;

    private Image PowerFill;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + transform.forward, interactSphereRadius);
    }

    void Awake()
    {
        networkIdentity = GetComponent<NetworkIdentity>();

        GameObject pbf = GameObject.Find("Power Bar");
        if (pbf != null)
        {
            PowerFill = pbf.GetComponent<Image>();
        }
    }

    private GameObject ClosestInteractableObject(){
        GameObject ret = null;
        Vector3 C = transform.position + transform.forward;
        if(Physics.CheckSphere(C, interactSphereRadius, INTERACTIVE)){
            Collider[] found = Physics.OverlapSphere(C, interactSphereRadius, INTERACTIVE);
            var orderedFound = found.OrderBy(c =>
            {
                return (c.gameObject.transform.position - C).sqrMagnitude;
            });

            ret = orderedFound.ElementAt(0).gameObject;
        }

        return ret;
    }

    [Command]
    public void CmdGrab()
    {
        GameObject obj = ClosestInteractableObject();
        if(obj){

            Debug.LogFormat("Grab the {0}!", obj.name);

            NetworkIdentity objectIdentity = obj.GetComponent<NetworkIdentity>();
            NetworkConnection ownerConnection = objectIdentity.clientAuthorityOwner;
            if(ownerConnection != null){
                if(ownerConnection == networkIdentity.connectionToClient){
                    Debug.Log("Already the owner");
                }
            } else {
                Debug.Log("NO OWNER, GIMMIE DAT");

                objectIdentity.localPlayerAuthority = true;
                objectIdentity.AssignClientAuthority(networkIdentity.connectionToClient);

                HeldObject = obj.GetComponent<HoldableItem>().PickUp(gameObject);
            }
        }
    }

    public void Charge()
    {
        if (ChargeLevel <= ChargeMax)
        {
            ChargeLevel += Time.deltaTime * ThrowSpeed;
        }

        PowerFill.fillAmount = ChargeLevel / 100;
    }


    public void Release()
    {
        HoldableItem releasedItem = HeldObject.Release();
        Vector3 TossDirection =
            (transform.forward * ChargeLevel) +
            (transform.up * 3.0f);
        Rigidbody rb = releasedItem.gameObject.GetComponent<Rigidbody>();
        rb.AddForce(TossDirection, ForceMode.Impulse);
        ChargeLevel = 0;
        PowerFill.fillAmount = 0;

        HeldObject = null;
    }
}
