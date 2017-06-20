/*==============================================================================
Copyright (c) 2010-2014 Qualcomm Connected Experiences, Inc.
All Rights Reserved.
Confidential and Proprietary - Protected under copyright and other laws.
==============================================================================*/

using UnityEngine;
using Prototype.NetworkLobby;
namespace Vuforia
{
    /// <summary>
    /// A custom handler that implements the ITrackableEventHandler interface.
    /// </summary>
    public class MultiPlayerTrackableEventHandler : MonoBehaviour,
                                                ITrackableEventHandler
    {
        #region PRIVATE_MEMBER_VARIABLES

        private TrackableBehaviour mTrackableBehaviour;
        private bool fistTimeDetect = false;
        #endregion // PRIVATE_MEMBER_VARIABLES



        #region UNTIY_MONOBEHAVIOUR_METHODS

        void Start()
        {
            mTrackableBehaviour = GetComponent<TrackableBehaviour>();
            if (mTrackableBehaviour)
            {
                mTrackableBehaviour.RegisterTrackableEventHandler(this);
            }
            fistTimeDetect = false;
        }

        #endregion // UNTIY_MONOBEHAVIOUR_METHODS

        void OnDisable()
        {
        }


        #region PUBLIC_METHODS

        /// <summary>
        /// Implementation of the ITrackableEventHandler function called when the
        /// tracking state changes.
        /// </summary>
        public void OnTrackableStateChanged(
                                        TrackableBehaviour.Status previousStatus,
                                        TrackableBehaviour.Status newStatus)
        {
            if (newStatus == TrackableBehaviour.Status.DETECTED ||
                newStatus == TrackableBehaviour.Status.TRACKED ||
                newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
            {
                OnTrackingFound();
            }
            else
            {
                OnTrackingLost();
            }
        }

        #endregion // PUBLIC_METHODS



        #region PRIVATE_METHODS


        private void OnTrackingFound()
        {
            if (!LobbyManager.s_Singleton.isReady)
            {
                LobbyManager.s_Singleton.OnImageTargetDetected();
            }
            else
            {
                GameManager.s_Instance.OnImageTargetFound();
            }
        }


        private void OnTrackingLost()
        {
            if (!LobbyManager.s_Singleton.isReady)
            {
                LobbyManager.s_Singleton.OnImageTargetLost();
            }
            else
            {
                GameManager.s_Instance.OnImageTargetLost();
            }
          }

        #endregion // PRIVATE_METHODS
    }
}
