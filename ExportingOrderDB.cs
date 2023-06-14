using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExportingOrderDB : MonoBehaviour
{
    public static List<ExportingOrder> ExpOrderList = new List<ExportingOrder>();
    public static List<GameObject> ExportableObjectList = new List<GameObject>();
    public static int ExportingID = 0;
    void Start()
    {

    }

    // Export listte ADD diyerek araca yüklenecek malları seçtik. SEND ettikten sonra varış zamanı dolana kadar liste temizlenemeyecek. Bu süreçte iptal edilemez
    // Arac vardıktan sonra seçilen ürünler listeden kalkar. yeni yükleme yapılabilir.
    // Araç ulaşmadan listede ürünler üzerinde sayaç görünecek sonra biter ve temizlenir.
    //Varış sonucu para cüzdana girer, nakliye ödenir, eğer gecikme var ise belirlenecek bir oranda kesinti olmalı .
    void Update()
    {

    }

    public static void AddToExportList(int expObjID, int orderID, int itemid, float loadQty, float earnings, float arrivaltime, Button addBtn, float remQty)
    {
        ExpOrderList.Add(new ExportingOrder(expObjID, orderID, itemid, loadQty, earnings, arrivaltime, addBtn, remQty));
        ExportingID++;
    }

    public static ExportingOrder GetExportInfo(int expID)
    {
        return ExpOrderList.Find(x => x.export_id == expID);
    }

    /// <summary>
    /// Araca yüklenen ürünlerin hacimlerini toplar
    /// </summary>
    /// <returns></returns>
    public static float SumAllCargoList_Volume()
    {
        float sum = 0;
        float qty;

        for (int i = 0; i < ExpOrderList.Count; i++)
        {
            sum += ExpOrderList[i].loadedQty * ItemDatabase.GetItem(ExpOrderList[i].item_id).stats["volume"];//araca yüklenen ürünlerin hacimleri

        }


        return sum;
    }

    /// <summary>
    /// Araca yüklenen ürünlerin değerleri (zamanında ulaşırsa kazanılacak para) hesaplar. Eklenen miktar kadar (qty*price)
    /// </summary>
    /// <returns></returns>
    public static float SumAllCargoList_Earnings()
    {
        float sum = 0;
        float qty;

        for (int i = 0; i < ExpOrderList.Count; i++)
        {
            sum += ExpOrderList[i].earnings;//araca yüklenen ürünlerin hacimleri

        }


        return sum;
    }

    /// <summary>
    /// Nakliye tipi değiştiğinde sevk olacak listeyi sıfırlar. Yazılan miktarlar geri döner
    /// </summary>
    public static void DeleteAllExportList()
    {
        if (ExpOrderList.Count > 0) // çoklu yükleme listesi (direkt araç için)
        {
            Debug.Log("SEVK CHEKCLIST:" + ExpOrderList.Count);
            for (int i = 0; i < ExpOrderList.Count; i++)//Sevk listesi
            {


                for (int x = 0; x < ExportableObjectList.Count; x++) // SevkEdilecekObjeler eski miktara geri alacak.(düzeltme yapacak) (Genel Obje - main)
                {
                    if (ExpOrderList[i].orderid == ExportableObjectList[x].GetComponent<ExportObjectProperty>().orderId && ExpOrderList[i].delivered == false) // sevk olmayanlar (1 sipariş 3 parçada yükleme yazılmış fakat 2 si sevk oldu 1 i olmadı-
                    { // if (ExpOrderList[i].orderid == ExportableObjectList[x].GetComponent<ExportObjectProperty>().orderId)
                        int id = ExportableObjectList[x].GetComponent<ExportObjectProperty>().orderId;
                        float lastQuantity = orderDatabase.GetCollectionOrderId(id).quantity - orderDatabase.GetCollectionOrderId(id).exportQuantity;//elde kalan miktar
                        ExportableObjectList[x].GetComponent<ExportObjectProperty>().OrderQty = lastQuantity;//güncel miktarı düzelt (sipariş-sevk miktar)
                        ExportableObjectList[x].GetComponent<ExportObjectProperty>().amountQty.text = lastQuantity.ToString(); // text ekranını düzelt

                        Debug.Log("SEVK ÖNCESİ BAKİYE:id" + id + "#" + lastQuantity + "/" + orderDatabase.GetCollectionOrderId(id).quantity + "/" + orderDatabase.GetCollectionOrderId(id).exportQuantity + "@@" + ExpOrderList.Count);
                        ExpOrderList.RemoveAt(i); //3
                    }
                }

                //ExpOrderList.RemoveAt(i); 1

                //ExpOrderList.RemoveAt(i); 2
            }
        }


    }
}
