using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Juniverse.Model;

public class ActiveQuestItemUI : MonoBehaviour
{
    [SerializeField] Text _title;
    [SerializeField] Text _description;
    [SerializeField] Text _visitTimeTxt;
    [SerializeField] GameObject _triggerIcon;
    [SerializeField] Button _location;
    [SerializeField] Text _rewardTxt;
    [SerializeField] RectTransform _rewardsContainer;

    Coroutine coroutine;

    DateTime _visitTime;

    // Use this for initialization
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
    }
    public void FillItemUI(string title, string description, ActiveQuestTimeState time, RewardsData rewards, Location mapLocation, bool isTrigger,bool bUpdate)
    {
        _title.text = title;
        _description.text = description;
        _visitTime = time.StartTime;

        if(!bUpdate)
            FillRewards(rewards);

        switch (time.State)
        {
            case ActiveQuestState.NotAttended:
                coroutine = StartCoroutine(UpdateVisitTimeText(DeviceManager.instance.UiItemsUpdateRate));
                break;
            case ActiveQuestState.Attended:
                StartQuest();
                break;
            case ActiveQuestState.SessionStarted:
                UpdateQuestStartTime(time.StartTime, true);
                break;
        }
        //Todo Handle _location button onclick
        _triggerIcon.SetActive(isTrigger);
    }

    void FillRewards(RewardsData rewardsData)
    {
        Text rewardTxt;
        //Filling XP
        rewardTxt = Instantiate(_rewardTxt);
        rewardTxt.transform.SetParent(_rewardsContainer.transform);
        rewardTxt.transform.localScale = Vector3.one;
        rewardTxt.text = "XP: " + rewardsData.XP.ToString();

        //Filling Junibucks
        if (rewardsData.Currency > 0)
        {
            rewardTxt = Instantiate(_rewardTxt);
            rewardTxt.transform.SetParent(_rewardsContainer.transform);
            rewardTxt.transform.localScale = Vector3.one;
            rewardTxt.text = "Cash: " + (rewardsData.Currency.ToString());
        }

        //Filling SkillPoints
        if (rewardsData.SkillPoints.Count > 0)
        {
            foreach (RewardSkillPoint skill in rewardsData.SkillPoints)
            {
                rewardTxt = Instantiate(_rewardTxt);
                rewardTxt.transform.SetParent(_rewardsContainer.transform);
                rewardTxt.transform.localScale = Vector3.one;
                //TODO: Add Skill icon
                rewardTxt.text = skill.Skill.ToString() + " Points: " + skill.Amount;
            }
        }

        //Filling Items
        if (rewardsData.Items.Count > 0)
        {
            foreach (string itemID in rewardsData.Items)
            {
                rewardTxt = Instantiate(_rewardTxt);
                rewardTxt.transform.SetParent(_rewardsContainer.transform);
                rewardTxt.transform.localScale = Vector3.one;
                //TODO: Add item icon
                rewardTxt.text = (DeviceManager.instance.GameData.Items[itemID].Name.GetLocalized());
            }
        }
    }

    IEnumerator UpdateVisitTimeText(float updateRate)
    {
        TimeSpan timeDifference = _visitTime - DateTime.UtcNow.AddHours(DeviceManager.instance.GameData.Configurations.TimeZone);
        int minutesToAvailableSession = (int)timeDifference.TotalMinutes;
        if (timeDifference.TotalSeconds > 0)
        {
            _visitTimeTxt.text = minutesToAvailableSession.ToString();
            yield return new WaitForSeconds(updateRate);
            coroutine = StartCoroutine(UpdateVisitTimeText(updateRate));
        }
        else
        {
            _visitTimeTxt.text = "Delayed";
            if(coroutine != null)
                StopCoroutine(coroutine);
            yield return null;
        }
    }

    public void UpdateQuestStartTime(DateTime time, bool hasSessionStarted = false)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);

        if(hasSessionStarted)
        {
            _visitTimeTxt.text = "Now";
            _visitTimeTxt.color = Color.red;
        }
        else
        {
            _visitTime = time;
            coroutine = StartCoroutine(UpdateVisitTimeText(DeviceManager.instance.UiItemsUpdateRate));
        }
    }

    public void StartQuest()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        _visitTimeTxt.color = Color.black;
        _visitTimeTxt.text = "Active";
    }
}
