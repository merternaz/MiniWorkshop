using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System;

public class ExportLogParameter : MonoBehaviour
{
    public Dropdown trackList;
    public Text selc,loadPercent,loadingAmount,loadCashValue,transferCost;
    public Image loadImage,sendingImage;
    public Button SendCargoButton;
    public static float LoadCap,transportCost,transportTime,currentLoad=0,currentCashValue,currentCustomerExp,currentSectorExp;
    private bool allowTransport = false;
    private float time,arrivedTime,lateTransferPenalty=0.15f;// transfer gecikme cezası %15
    private float warehouseQty;
    DatabaseReference warehouseRef,custExpRef,sectorExpRef;

    private int itemID;
    // Start is called before the first frame update

    private void Awake()
    {
        GetTrackListData(trackList);
        
    }
    void Start()
    {
        // List<Dropdown.OptionData> a = LogisticsOptionsDB.GetLogisticsInfo(1).LogName;//trackList.GetComponent<Dropdown>().options;
        warehouseRef= auth.userReferance.Child(auth.USER_ID).Child("warehouse");
        custExpRef = auth.userReferance.Child(auth.USER_ID).Child("customerExperiance");
        sectorExpRef = auth.userReferance.Child(auth.USER_ID).Child("customerExperiance");

        trackList.onValueChanged.AddListener(delegate
        {
            TrackListDropDown(trackList);
        });//Dropdown objesine nakliye çeşitlerini ekler


        SendCargoButton.onClick.AddListener(delegate
        {
            Debug.Log("SEND BASILDI");
            trackList.gameObject.SetActive(false);
            SendTransport();
        });//SEND butonu ile sevk başlatır. Butonu sürekli dinler. Her seçimde fonksyon çalışır (AddListener ile)

        custExpRef.ValueChanged += GetCustomerAndSectorExperiance;
    }

    // Update is called once per frame
    void Update()
    {
        loadImage.fillAmount = currentLoad / LoadCap;
        loadingAmount.text = currentLoad.ToString()+"/"+ LoadCap.ToString();
        loadPercent.text = "% "+(currentLoad * 100 / LoadCap).ToString("#.#");
        transferCost.text = transportCost.ToString();
        loadCashValue.text = currentCashValue.ToString();

        if (allowTransport)// Transfer butonuna basıldı
        {
            time += Time.deltaTime;
            sendingImage.fillAmount = time / transportTime;

            if (time >= transportTime)
            {
                custExpRef.ValueChanged += GetCustomerAndSectorExperiance;

                arrivedTime = time + simulation.timer;//varış zamanı 

                CalculateEarnings(arrivedTime);// varış sonrası kazanç ve gider hesapla
                warehouseRef.ValueChanged += WarehouseUpdate; // ambarı güncelle (ürünleri ambardan düşür)

                time = 0;
                arrivedTime = 0;
                currentLoad = 0;
                sendingImage.fillAmount = 0;
                trackList.gameObject.SetActive(true);
                allowTransport = false;
            }
        }
    }

    public void TrackListDropDown(Dropdown dd)//Yükleme türü seçimi
    {
        int i = dd.GetComponent<Dropdown>().value;
        selc.text = LogisticsOptionsDB.GetLogisticsInfo(i).LogName;// yükleme adı (tipten alacak)
        LoadCap= LogisticsOptionsDB.GetLogisticsInfo(i).LogDetails["capacity"];// yükleme kapasitesi
        transportCost= LogisticsOptionsDB.GetLogisticsInfo(i).LogDetails["cost"];// yükleme maliyeti
        transportTime= LogisticsOptionsDB.GetLogisticsInfo(i).LogDetails["arrival"];// varış zamanı
        //dd.GetComponent<Dropdown>().options[i].text= LogisticsOptionsDB.GetLogisticsInfo(i).LogName;

       
        currentLoad = 0;
        currentCashValue = 0;
        ExportingOrderDB.DeleteAllExportList();//listenin tamamını iptal et miktarları eski haline getir

        for (int x = 0; x < ExportingOrderDB.ExpOrderList.Count; x++)
        {
            ExportingOrderDB.ExpOrderList[x].addButton.gameObject.SetActive(true);//gizlenen ADD butonları görünür olur

        }
    }

    void GetTrackListData(Dropdown dd)
    {
        for(int i = 0; i < LogisticsOptionsDB.logistics.Count; i++)
        {
            dd.GetComponent<Dropdown>().options[i].text = LogisticsOptionsDB.GetLogisticsInfo(i).LogName;
            dd.GetComponent<Dropdown>().captionText.text= LogisticsOptionsDB.GetLogisticsInfo(i).LogName;
        }
    }

    public void SendTransport()
    {
        allowTransport = true;
    }

    private void GetCustomerAndSectorExperiance(object sender,ValueChangedEventArgs v)
    {
        var refValue = v.Snapshot.Value as Dictionary<string, object>;

        foreach(var parameter in v.Snapshot.Children)
        {
            var param = parameter.Value as Dictionary<string, object>;

            currentCustomerExp = (float)Convert.ToDouble(param["customerExperiance"].ToString());
            currentSectorExp = (float)Convert.ToDouble(param["sectorExperiance"].ToString());
        }
        
          /*  currentCustomerExp = (float)Convert.ToDouble(refValue["customerExperiance"].ToString());
            currentSectorExp= (float)Convert.ToDouble(refValue["sectorExperiance"].ToString());*/

        custExpRef.ValueChanged -= GetCustomerAndSectorExperiance;
    }

    /// <summary>
    /// siparişten kazancı kasaya ekleyecek (Ceza ve transfer maliyetini düşerek)
    /// </summary>
    /// <param name="arrTime"></param>
    void CalculateEarnings(float arrTime) // kazanç (Termine göre kazanç ve gecikmeye göre katsaylı kesinti
    {
        database DB = new database();
        float totalEarnings=0;
        float orderArrivalTime,wallet;
        int itemid;

        wallet = infobar.cash;

        for(int i = 0; i < ExportingOrderDB.ExpOrderList.Count; i++)
        {
            // varış zamanına göre termini geçerse kesinti yap--ok
            // sevk miktarları kadar ambardan miktar azalt (tüketim) --- warehouseConsumption gibi
            orderArrivalTime = orderDatabase.GetCollectionOrderId(ExportingOrderDB.ExpOrderList[i].orderid).lastDemandTime;//siparişin müşteriye varış istenieln zamanı

            ExportingOrderDB.ExpOrderList[i].delivered = true;

            if (arrTime<= orderArrivalTime)//termin süresi varış süresinden büyükse (erken varır ise)
            {
                totalEarnings += ExportingOrderDB.ExpOrderList[i].earnings;
                if (currentCustomerExp < 100f)
                {
                    currentCustomerExp += 0.01f;
                }
                else
                {
                    currentCustomerExp = 100f;
                }

                StartCoroutine(DB.UpdateCustomerExperiance(auth.USER_ID, currentCustomerExp));
                
            }
            else
            {
                totalEarnings += ExportingOrderDB.ExpOrderList[i].earnings*(1-lateTransferPenalty); // kazanctan ceza oranı kadar kesinti olacak
                currentCustomerExp -= 0.005f;

                if (currentCustomerExp > -50f)
                {
                    currentCustomerExp -= 0.005f;
                }
                else
                {
                    currentCustomerExp = -50f;
                }
            }



        }

        // işlemi biten objeleri (sipariş sevk tamamı biten objeleri gizle/kapat
        for(int x = 0; x < ExportingOrderDB.ExportableObjectList.Count; x++)// sevk edilebilir objeler(siparişler)
        {
            if (ExportingOrderDB.ExportableObjectList[x].GetComponent<ExportObjectProperty>().OrderQty == 0)//siparişten kalan 0 ise tamamlanmıştır
            {
                for(int y = 0; y < customerOrder.ordersList.Count; y++)// Order Panel üzerinde eklenen sipariş Objeleri kontrol et
                {
                    if (ExportingOrderDB.ExportableObjectList[x].GetComponent<ExportObjectProperty>().orderId == customerOrder.ordersList[y].GetComponent<ordersObjectProperties>().orderid)
                    {
                        //tamamlanan sevkiyatın siparişi ile Orjinal siparişi bul ve her iki objeyide destroy et

                        //customerOrder.ordersList[y].gameObject.SetActive(false);//                        
                        
                       
                        //ExportingOrderDB.ExportableObjectList[x].gameObject.SetActive(false);
                        

                        for(int z = 0; z < JobDatabase.jobObjectList.Count; z++)// İŞ EMRİ LİSTESİ
                        {
                            if (customerOrder.ordersList[y].GetComponent<ordersObjectProperties>().orderid == JobDatabase.jobObjectList[z].GetComponent<JobObjectScript>().orderid)
                            {
                                // JobDatabase.jobObjectList[z].gameObject.SetActive(false); // iş emrini gizle
                               // Destroy(JobDatabase.jobObjectList[z].gameObject);     // obje bulamadı hatası ile                            
                               //  Destroy(customerOrder.ordersList[y].gameObject); // stockCheck.cs 139 satırda hatası var
                                Destroy(ExportingOrderDB.ExportableObjectList[x].gameObject); 
                                Debug.Log("ORDER SAYISI 2 =" + customerOrder.ordersList.Count);
                            }
                        }
                    }
                }
            }
        }

        StartCoroutine(DB.UpdateBusinessCash(auth.USER_ID, wallet + totalEarnings - transportCost));//cüzdan güncelle
        simulation.dailyExportEarns.Add(totalEarnings);//günlük rapora eklenecek kazanç
        simulation.dailyExportCost.Add(transportCost);

        Debug.Log("SEVKİYAT KAZANC:" + (totalEarnings-transportCost));
        
    }

    void DestroyFinishedOrdersAndExportObj(List<ExportObjectProperty> ExportableObject,List<ordersObjectProperties> OrdersObject)
    {
        for (int x = 0; x < ExportingOrderDB.ExportableObjectList.Count; x++)
        {
            if (ExportingOrderDB.ExportableObjectList[x].GetComponent<ExportObjectProperty>().OrderQty == 0)//siparişten kalan 0 ise tamamlanmıştır
            {
                for (int y = 0; y < customerOrder.ordersList.Count; y++)// Order Panel üzerinde eklenen sipariş Objeleri kontrol et
                {
                    if (ExportingOrderDB.ExportableObjectList[x].GetComponent<ExportObjectProperty>().orderId == customerOrder.ordersList[y].GetComponent<ordersObjectProperties>().orderid)
                    {
                        //tamamlanan sevkiyatın siparişi ile Orjinal siparişi bul ve her iki objeyide destroy et

                        Destroy(customerOrder.ordersList[y].gameObject);
                        Destroy(ExportingOrderDB.ExportableObjectList[x].gameObject);

                    }
                }
            }
        }
    }
    private void WarehouseUpdate(object sender,ValueChangedEventArgs WH_UPDATE)
    {
        var whKeys = WH_UPDATE.Snapshot.Value as Dictionary<string, object>;
        database DB = new database();
        foreach (var wh_property in WH_UPDATE.Snapshot.Children) // katman içindeki tip ve değerler
        {
            for(int i = 0; i < ExportingOrderDB.ExpOrderList.Count; i++)
            {
                int itemid = ExportingOrderDB.ExpOrderList[i].item_id;
                float qtyLoaded = ExportingOrderDB.ExpOrderList[i].loadedQty;
                int orderid= ExportingOrderDB.ExpOrderList[i].orderid;


                if (wh_property.Key.Equals(itemid.ToString()))
                {

                    //   this.gameObject.transform.GetComponentInParent<matPropertys>().inWarehouse = true; // itemi işaretle varsa 

                    var values = wh_property.Value as Dictionary<string, object>;
                    orderDatabase.GetCollectionOrderId(orderid).exportQuantity += qtyLoaded; // siparişin yüklenen miktarı (sevk miktarı). Sonrasında yükleme tipi değişir ise en başa dönmemesi için

                    warehouseQty = (float)Convert.ToDouble(values["quantity"].ToString()); // AMBAR MİKTARI
                    StartCoroutine(DB.UpdateItemQuantity(auth.USER_ID, itemid, warehouseQty - qtyLoaded));
                    Debug.Log("SEVKİYAT AMBAR::"+ ExportingOrderDB.ExpOrderList[i].orderid+"/"+ orderDatabase.GetCollectionOrderId(orderid).exportQuantity);

                   // break;
                }
            }

            
        }

        warehouseRef.ValueChanged -= WarehouseUpdate;
    }

   
}
