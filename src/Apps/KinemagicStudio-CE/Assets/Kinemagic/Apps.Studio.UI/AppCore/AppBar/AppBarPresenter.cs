using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EngineLooper;
using R3;
using UnityEngine;

namespace Kinemagic.Apps.Studio.UI.AppCore
{
    public sealed class AppBarPresenter : IDisposable, IInitializable, ITickable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIViewContext _context;
        private readonly AppBarView _view;

        private float _previousFrameTime;
        private float _averageDeltaTime;

        public AppBarPresenter(UIViewContext context, AppBarView view)
        {
            _context = context;
            _view = view;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Initialize()
        {
            _context.CurrentPage
                .Where(pageType => pageType != UIPageType.Unknown)
                .Subscribe(pageType =>
                {
                    if (pageType == UIPageType.MotionCapture)
                    {
                        UpdateIpAddress();
                        _view.SetLocalIpAddressVisibility(isVisible: true);
                    }
                    else
                    {
                        _view.SetLocalIpAddressVisibility(isVisible: false);
                    }
                })
                .AddTo(_disposables);

            _view.LocalIpUpdateButtonClicked
                .Subscribe(_ => UpdateIpAddress())
                .AddTo(_disposables);
        }

        public void Tick()
        {
            UpdateFrameRate();
        }

        private void UpdateFrameRate()
        {
            var currentFrameTime = Time.realtimeSinceStartup;
            var deltaTime = currentFrameTime - _previousFrameTime;
            _previousFrameTime = currentFrameTime;

            _averageDeltaTime += deltaTime;
            if (Time.frameCount % 10 == 0)
            {
                _averageDeltaTime /= 10;
                _view.SetFrameRateValue(1f / _averageDeltaTime);
                _averageDeltaTime = 0f;
            }
        }

        private void UpdateIpAddress()
        {
            var ipAddresses = GetIpAddresses();
            var ipAddress = ipAddresses.Count > 0 ? ipAddresses[0] : "";
            _view.SetLocalIpAddress(ipAddress);
        }

        private List<string> GetIpAddresses()
        {
            var ipAddresses = new List<string>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addressInfo in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (!IPAddress.IsLoopback(addressInfo.Address) && addressInfo.IsDnsEligible &&
                        addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddresses.Add(addressInfo.Address.ToString());
                    }
                }
            }

            return ipAddresses;
        }
    }
}
