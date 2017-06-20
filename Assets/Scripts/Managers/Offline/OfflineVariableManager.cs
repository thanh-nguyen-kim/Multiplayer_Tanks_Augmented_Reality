using UnityEngine;
public class OfflineVariableManager : MonoBehaviour
{
    private static OfflineVariableManager instance = null;
    public static OfflineVariableManager Instance
    {
        get { return instance; }
    }

    public float Coin
    {
        get
        {
            coin = PlayerPrefs.GetFloat("coin", 0);
            return coin;
        }

        set
        {
            this.coin = value;
            PlayerPrefs.SetFloat("coin", coin);
        }
    }

    private float coin;
    public int m_GameMode;
    public int m_MissionIndex;//this mission index ex: 1,2,3..
    void Start()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public int GetSkullAt(int index)
    {
        string skulls = PlayerPrefs.GetString("skull", "000000");
        if (index >= skulls.Length)
        {
            for (int i = skulls.Length; i <= index; i++)
                skulls += "0";
            PlayerPrefs.SetString("skull", skulls);
        }
        return int.Parse(skulls[index].ToString());
    }

    public int GetTotalSkull()
    {
        string skulls = PlayerPrefs.GetString("skull", "000000");
        int count = 0;
        for (int i = 0; i < skulls.Length; i++)
            count += int.Parse(skulls[i].ToString());
        count -= GetTotalSkullSpend();
        return count;
    }

    public void SetSkullAt(int index,int number)
    {
        string skulls = PlayerPrefs.GetString("skull", "000000");
        if(!(GetSkullAt(index)>number))
        {
            string tmp = skulls.Insert(index+1, number.ToString());
            skulls = tmp.Remove(index, 1);
            PlayerPrefs.SetString("skull", skulls);
        }
    }

    public void SetCurrentMissionSkull(int number)
    {
        SetSkullAt(m_MissionIndex,number);
    }

    public bool CheckPurchasedTank(int i)
    {
        string purchasedInfo = PlayerPrefs.GetString("PurchasedTanks", "100");
        if (i >= purchasedInfo.Length) return false;
        else
            if (purchasedInfo[i] != '0') return true;
        return false;
    }

    public void SavePurchaseTank(int index, int cost)
    {
        string purchasedInfo = PlayerPrefs.GetString("PurchasedTanks", "100");
        string tmp = purchasedInfo.Insert(index + 1,cost.ToString());
        purchasedInfo = tmp.Remove(index, 1);
        PlayerPrefs.SetString("PurchasedTanks", purchasedInfo);
    }

    private int GetTotalSkullSpend() {
        int total = 0;
        string purchasedInfo = PlayerPrefs.GetString("PurchasedTanks", "100");
        for(int i=0;i<purchasedInfo.Length; i++)
        {
            int tmp=int.Parse(purchasedInfo[i].ToString())-1;
            if (tmp > 0) total += tmp;
        }
        return total;
    } 

}
