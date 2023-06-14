using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class selectingSlot : MonoBehaviour
{
    public static bool s1 = false,s2=false,s3=false,updateBackground=false;
    public static string slotName;
    public Transform selectingTransform;
    // Start is called before the first frame update
    public void Slot1()
    {
        s1 = true;
        s2 = false;
        s3 = false;
        slotName = "slots1";
        BUYMachine();
    }

    public void Slot2()
    {
        s1 = false;
        s2 = true;
        s3 = false;
        slotName = "slots2";
        BUYMachine();

    }


    public void Slot3()
    {
        s1 = false;
        s2 = false;
        s3 = true;
        slotName = "slots3";
        BUYMachine();
    }

    public void BUYMachine()
    {
        database db = new database();

        if (buyMachine.hasMachine == true)// makina varsa yer de?i?tirir,yoksa ekler
        {
            StartCoroutine(db.UpdateMachines(auth.USER_ID, buyMachine.id, buyMachine.name, 
                buyMachine.cost, buyMachine.speed, buyMachine.sector, buyMachine.energy, slotName));
        }
        else
        {
            db.InsertMachines(auth.USER_ID, buyMachine.id, buyMachine.name,
                buyMachine.cost, buyMachine.speed, buyMachine.sector, buyMachine.energy, slotName);
        }

                  
       
        
        updateBackground = true; // simulation.cs scriptine UPDATE() fonks. içinde kontrol edilir. TRUE ise hangi slota hangi makina eklenecek onu belirler ekran? günceller
    }

    private void LoadMachine(Transform background, string machineName)
    {
        GameObject obj = Resources.Load<GameObject>("machines/" + machineName);
        Instantiate(obj, background);


    }

    public void CloseMenu()
    {
        selectingTransform.transform.gameObject.SetActive(false);
    }
}
