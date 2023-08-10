using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace io.agora.mixed.demo
{

    public class MixedScene : MonoBehaviour
    {
        public Text LogText;
        //rtc
        public InputField RtcAppIdInput;
        public InputField RtcTokenInput;
        public InputField RtcChannelInput;
        //rtm
        public InputField RtmAppIdInput;
        public InputField RtmTokenInput;
        public InputField RtmUidInput;
        public InputField RtmChannelInput;
        public InputField RtmMessageInput;

        public agora_gaming_rtc.IRtcEngine RtcEngine;
        public Agora.Rtm.IRtmClient RtmClient;

        //rtc
        public void OnRtcButton()
        {
            if (RtcEngine == null)
            {
                var appID = this.RtcAppIdInput.text;
                if (appID == "")
                {
                    this.UpdateLog("[RTC]", "Rtc appid is null");
                    return;
                }

                var channel = this.RtcChannelInput.text;
                if (channel == "")
                {
                    this.UpdateLog("[RTC]", "Rtc channel is null");
                    return;
                }

                var token = this.RtcTokenInput.text;

                RtcEngine = agora_gaming_rtc.IRtcEngine.getEngine(appID);
                RtcEngine.OnJoinChannelSuccess = this.OnJoinChannelSuccess;
                RtcEngine.OnLeaveChannel = this.OnLeaveChannel;
                RtcEngine.OnUserJoined = this.OnUserJoined;
                RtcEngine.OnUserOffline = this.OnUserOffline;
                RtcEngine.SetLogFilter(
                    agora_gaming_rtc.LOG_FILTER.DEBUG |
                    agora_gaming_rtc.LOG_FILTER.INFO |
                    agora_gaming_rtc.LOG_FILTER.WARNING |
                    agora_gaming_rtc.LOG_FILTER.ERROR |
                    agora_gaming_rtc.LOG_FILTER.CRITICAL);

                RtcEngine.EnableAudio();
                RtcEngine.EnableVideo();
                RtcEngine.EnableVideoObserver();
                RtcEngine.SetClientRole(agora_gaming_rtc.CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
                var ret = RtcEngine.JoinChannelByKey(token, channel);
                this.UpdateLog("[RTC]", "RtcEngine JoinChannel: " + ret);

            }
        }

        public void Update()
        {

        }

        async public void OnRtmButton()
        {
            if (RtmClient == null)
            {
                var appId = this.RtmAppIdInput.text;
                if (appId == "")
                {
                    this.UpdateLog("[RTM]", "rtm appid is null");
                    return;
                }


                var username = this.RtmUidInput.text;
                if (username == "")
                {
                    this.UpdateLog("[RTM]", "rtm uid is null");
                    return;
                }

                var channel = this.RtmChannelInput.text;
                if (channel == "")
                {
                    this.UpdateLog("[RTM]", "rtm channel is null");
                    return;
                }

                var token = this.RtmTokenInput.text;

                Agora.Rtm.RtmConfig config = new Agora.Rtm.RtmConfig();
                config.appId = appId;
                config.userId = username;
                config.presenceTimeout = 30;
                config.useStringUserId = true;

                try
                {
                    RtmClient = Agora.Rtm.RtmClient.CreateAgoraRtmClient(config);
                }
                catch (Agora.Rtm.RTMException e)
                {
                    this.UpdateLog("[RTM]", "rtmClient.init error  ret:" + e.Status.ErrorCode);
                }


                if (RtmClient != null)
                {
                    //add observer
                    RtmClient.OnMessageEvent += this.OnMessageEvent;
                    RtmClient.OnPresenceEvent += this.OnPresenceEvent;
                    RtmClient.OnTopicEvent += this.OnTopicEvent;
                    RtmClient.OnStorageEvent += this.OnStorageEvent;
                    RtmClient.OnLockEvent += this.OnLockEvent;
                    RtmClient.OnConnectionStateChange += this.OnConnectionStateChange;
                    RtmClient.OnTokenPrivilegeWillExpire += this.OnTokenPrivilegeWillExpire;
                    this.UpdateLog("[RTM]", "rtmClient init success");

                    var loginResult = await RtmClient.LoginAsync(token);
                    this.UpdateLog("[RTM]", "rtmClient login: " + loginResult.Status.ErrorCode);


                    Agora.Rtm.SubscribeOptions subscribeOptions = new Agora.Rtm.SubscribeOptions();
                    subscribeOptions.withMessage = true;
                    subscribeOptions.withMetadata = true;
                    subscribeOptions.withPresence = true;
                    subscribeOptions.withLock = true;
                    var result = await RtmClient.SubscribeAsync(channel, subscribeOptions);
                    this.UpdateLog("[RTM]", "rtmClient SubscribeAsync ErrorCode: " + result.Status.ErrorCode);
                }
            }
        }

        async public void OnPublishButton()
        {
            var message = this.RtmMessageInput.text;
            if (message == "")
            {
                this.UpdateLog("[RTM]", "message is null");
                return;
            }

            var channel = this.RtmChannelInput.text;
            if (channel == "")
            {
                this.UpdateLog("[RTM]", "rtm channel is null");
                return;
            }

            this.RtmMessageInput.text = "";
            var result = await RtmClient.PublishAsync(channel, message, new Agora.Rtm.PublishOptions());
            this.UpdateLog("[RTM]", "rtm publish: " + result.Status.ErrorCode);
        }

        public void OnDestroy()
        {
            if (RtcEngine != null)
            {
                RtcEngine.LeaveChannel();

                RtcEngine.DisableVideoObserver();
                agora_gaming_rtc.IRtcEngine.Destroy();
            }

            if (RtmClient != null)
            {
                RtmClient.Dispose();
            }
        }

        public void UpdateLog(string tag, string log)
        {
            var appendString = tag + " : " + log;
            Debug.Log(appendString);
            var logString = this.LogText.text;
            if (logString.Length > 300)
            {
                logString = logString.Substring(logString.Length - 50);
            }
            logString += ("\n" + appendString);
            this.LogText.text = logString;
        }

        public string GetChannelName()
        {
            return this.RtcChannelInput.text;
        }

        #region -- Video Render UI Logic ---

        public void MakeVideoView(uint uid, string channelId = "")
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                return; // reuse
            }

            // create a GameObject and assign to this new user
            var videoSurface = MakeImageSurface(uid.ToString());
            if (ReferenceEquals(videoSurface, null)) return;
            // configure videoSurface
            if (uid == 0)
            {
                videoSurface.SetForUser(uid);
            }
            else
            {
                videoSurface.SetForUser(uid);
            }

            videoSurface.SetEnable(true);
        }

        // VIDEO TYPE 1: 3D Object
        private agora_gaming_rtc.VideoSurface MakePlaneSurface(string goName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            var mesh = go.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                Debug.LogWarning("VideoSureface update shader");
                mesh.material = new Material(Shader.Find("Unlit/Texture"));
            }
            // set up transform
            go.transform.Rotate(-90.0f, 0.0f, 0.0f);
            go.transform.position = Vector3.zero;
            go.transform.localScale = new Vector3(0.25f, 0.5f, 0.5f);

            // configure videoSurface
            var videoSurface = go.AddComponent<agora_gaming_rtc.VideoSurface>();
            return videoSurface;
        }

        // Video TYPE 2: RawImage
        private agora_gaming_rtc.VideoSurface MakeImageSurface(string goName)
        {
            GameObject go = new GameObject();

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // to be renderered onto
            go.AddComponent<RawImage>();
            // make the object draggable
            go.AddComponent<UIElementDrag>();
            var canvas = this.gameObject;
            if (canvas != null)
            {
                go.transform.parent = canvas.transform;
                Debug.Log("add video view");
            }
            else
            {
                Debug.Log("Canvas is null video view");
            }

            // set up transform
            go.transform.Rotate(0f, 0.0f, 180.0f);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(2f, 3f, 1f);

            // configure videoSurface
            var videoSurface = go.AddComponent<agora_gaming_rtc.VideoSurface>();
            return videoSurface;
        }

        public void DestroyVideoView(uint uid)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                Destroy(go);
            }
        }

        #endregion


        #region rtm event
        public void OnMessageEvent(Agora.Rtm.MessageEvent @event)
        {
            string str = string.Format("OnMessageEvent channelName:{0} channelTopic:{1} channelType:{2} publisher:{3} message:{4} customType:{5}",
              @event.channelName, @event.channelTopic, @event.channelType, @event.publisher, @event.message.GetData<string>(), @event.customType);
            this.UpdateLog("[RTM]", str);
        }

        public void OnPresenceEvent(Agora.Rtm.PresenceEvent @event)
        {
            string str = string.Format("OnPresenceEvent: type:{0} channelType:{1} channelName:{2} publisher:{3}",
                @event.type, @event.channelType, @event.channelName, @event.publisher);
            this.UpdateLog("[RTM]", str);
        }

        public void OnStorageEvent(Agora.Rtm.StorageEvent @event)
        {
            string str = string.Format("OnStorageEvent: channelType:{0} storageType:{1} eventType:{2} target:{3}",
                @event.channelType, @event.storageType, @event.eventType, @event.target);
            this.UpdateLog("[RTM]", str);
            if (@event.data != null)
            {
                DisplayRtmMetadata(ref @event.data);
            }
        }

        public void OnTopicEvent(Agora.Rtm.TopicEvent @event)
        {
            string str = string.Format("OnTopicEvent: channelName:{0} publisher:{1}", @event.channelName, @event.publisher);
            this.UpdateLog("[RTM]", str);

            var topicInfoCount = @event.topicInfos == null ? 0 : @event.topicInfos.Length;
            if (topicInfoCount > 0)
            {
                for (var i = 0; i < topicInfoCount; i++)
                {
                    var topicInfo = @event.topicInfos[i];
                    var publisherCount = topicInfo.publishers == null ? 0 : topicInfo.publishers.Length;
                    string str1 = string.Format("|--topicInfo {0}: topic:{1} publisherCount:{2}", i, topicInfo.topic, publisherCount);
                    this.UpdateLog("[RTM]", str1);

                    if (publisherCount > 0)
                    {
                        for (var j = 0; j < publisherCount; j++)
                        {
                            var publisher = topicInfo.publishers[j];
                            string str2 = string.Format("  |--publisher {0}: userId:{1} meta:{2}", j, publisher.publisherUserId, publisher.publisherMeta);
                            this.UpdateLog("[RTM]", str2);
                        }
                    }
                }
            }
        }

        public void OnLockEvent(Agora.Rtm.LockEvent @event)
        {
            var count = @event.lockDetailList == null ? 0 : @event.lockDetailList.Length;
            string info = string.Format("OnLockEvent channelType:{0}, eventType:{1}, channelName:{2}, count:{3}", @event.channelType, @event.eventType, @event.channelName, count);
            this.UpdateLog("[RTM]", info);

            if (count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    var detail = @event.lockDetailList[i];
                    string info2 = string.Format("lockDetailList lockName:{0}, owner:{1}, ttl:{2}", detail.lockName, detail.owner, detail.ttl);
                    this.UpdateLog("[RTM]", info2);
                }
            }

        }

        public void OnConnectionStateChange(string channelName, Agora.Rtm.RTM_CONNECTION_STATE state, Agora.Rtm.RTM_CONNECTION_CHANGE_REASON reason)
        {
            string str1 = string.Format("OnConnectionStateChange channelName {0}: state:{1} reason:{2}", channelName, state, reason);
            this.UpdateLog("[RTM]", str1);
        }

        public void OnTokenPrivilegeWillExpire(string channelName)
        {
            string str1 = string.Format("OnTokenPrivilegeWillExpire channelName {0}", channelName);
            this.UpdateLog("[RTM]", str1);
        }

        private void DisplayRtmMetadata(ref Agora.Rtm.RtmMetadata data)
        {
            this.UpdateLog("[RTM]", "RtmMetadata.majorRevision:" + data.majorRevision);
            if (data.metadataItemsSize > 0)
            {
                foreach (var item in data.metadataItems)
                {
                    this.UpdateLog("[RTM]", string.Format("key:{0},value:{1},authorUserId:{2},revision:{3},updateTs:{4}", item.key, item.value, item.authorUserId, item.revision, item.updateTs));
                }
            }
        }
        #endregion

        #region rc eventhandler
        public void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
        {
            int build = 0;
            Debug.Log("Agora: OnJoinChannelSuccess ");
            UpdateLog("[RTC]", string.Format("sdk version: ${0}",
                agora_gaming_rtc.IRtcEngine.GetSdkVersion()));
            UpdateLog("[RTC]", string.Format("sdk build: ${0}",
              build));
            UpdateLog("[RTC]",
                string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                                channelName, uid, elapsed));
            MakeVideoView(0);
        }

        public void OnLeaveChannel(agora_gaming_rtc.RtcStats stats)
        {
            UpdateLog("[RTC]", "OnLeaveChannel");
        }

        public void OnReJoinChannelSuccess(string channelName, uint uid, int elapsed)
        {
            UpdateLog("[RTC]", "OnRejoinChannelSuccess");
        }


        public void OnUserJoined(uint uid, int elapsed)
        {
            UpdateLog("[RTC]", string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
            MakeVideoView(uid, GetChannelName());
        }


        public void OnUserOffline(uint uid, agora_gaming_rtc.USER_OFFLINE_REASON reason)
        {
            UpdateLog("[RTC]", string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
               (int)reason));
            DestroyVideoView(uid);
        }

        #endregion

    }

    public class UIElementDrag : EventTrigger
    {

        public override void OnDrag(PointerEventData eventData)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            base.OnDrag(eventData);
        }
    }


}
