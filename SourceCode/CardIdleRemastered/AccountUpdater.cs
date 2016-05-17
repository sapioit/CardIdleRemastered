﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CardIdleRemastered
{
    public class AccountUpdater
    {
        private AccountModel _account;
        private DispatcherTimer _tmSync;       
        private DispatcherTimer _tmCounter;        
        private TimeSpan _interval;
        private int _counter;

        public AccountUpdater(AccountModel account)
        {
            _account = account;

            _tmSync = new DispatcherTimer();
            _tmSync.Tick += SyncBanges;
            Interval = new TimeSpan(0, 5, 0);

            _tmCounter = new DispatcherTimer();
            _tmCounter.Tick += UpdateSecondCounter;
            _tmCounter.Interval = new TimeSpan(0, 0, 1);
        }

        public void Start()
        {            
            if (false == _tmSync.IsEnabled)
                _tmSync.Start();

            if (false == _tmCounter.IsEnabled)
            {
                _counter = 0;
                _tmCounter.Start();
            }            
        }

        public void Stop()
        {            
            _tmSync.Stop();            
            _tmCounter.Stop();
        }

        private void UpdateSecondCounter(object sender, EventArgs eventArgs)
        {            
            _counter++;
            int seconds = (int)Interval.TotalSeconds - _counter;
            if (seconds > 0)
            {
                var ts = TimeSpan.FromSeconds(seconds);
                _account.SyncTime = String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
            }
            else
                _account.SyncTime = "00:00";
        }

        public TimeSpan Interval
        {
            get { return _interval; }
            set
            {
                _interval = value;
                _tmSync.Interval = _interval;
            }
        }

        private async void SyncBanges(object sender, EventArgs eventArgs)
        {            
            _counter = 0;
            await Sync();            
        }

        public async Task Sync()
        {
            var tBadges = LoadBadgesAsync();
            var tProfile = LoadProfileAsync();
            await Task.WhenAll(tBadges, tProfile);
        }

        private async Task LoadProfileAsync()
        {
            var profile = await new SteamParser().LoadProfileAsync(_account.ProfileUrl);
            _account.BackgroundUrl = profile["BackgroundUrl"];
            _account.AvatarUrl = profile["AvatarUrl"];            
            _account.UserName = profile["UserName"];            
            _account.Level = profile["Level"];            
        }

        private async Task LoadBadgesAsync()
        {
            var badges = await new SteamParser().LoadBadgesAsync(_account.ProfileUrl);

            foreach (var badge in badges)
            {
                var b = _account.AllBadges.FirstOrDefault(x => x.AppId == badge.AppId);
                if (badge.RemainingCard > 0)
                {
                    if (b == null)
                        _account.AddBadge(badge);
                    else
                    {
                        b.RemainingCard = badge.RemainingCard;
                        b.HoursPlayed = badge.HoursPlayed;
                    }
                }
                else
                {
                    if (b != null)
                        _account.RemoveBadge(b);
                }
            }

            _account.UpdateTotalValues();
        }
    }
}
