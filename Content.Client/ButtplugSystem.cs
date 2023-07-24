using Content.Shared.Ball;
using Content.Shared.Paddle;
using Robust.Buttplug.Client;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;
using System.Threading.Tasks;
using Robust.Buttplug;
using System;

namespace Content.Client
{
    internal class ButtplugSystem : EntitySystem
    {
        public ButtplugClient Client { get; set; }
        public EntityUid Entity { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            Client = new("Robust Pong");
            Task.Run(async () =>
            {
                try
                {
                    await Client.ConnectAsync(new ButtplugWebsocketConnector(new("ws://localhost:8080")));
                    await Client.StartScanningAsync();
                }
                catch (Exception e)
                {
                    Log.Error($"{e}");
                }
            });

            SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);
            SubscribeLocalEvent<PaddleComponent, StartCollideEvent>(OnPaddleHit);
        }

        private void VibrateAll(float strength, int ms)
        {
            if (Client == null || !Client.Connected)
                return;

            _ = Task.Run(async () =>
            {
                // Enable
                foreach (var device in Client.Devices)
                {
                    await device.VibrateAsync(strength);
                }
                // Wait
                await Task.Delay(ms);
                // Disable
                foreach (var device in Client.Devices)
                {
                    await device.VibrateAsync(0);
                }
            });
        }

        private void OnPaddleHit(EntityUid uid, PaddleComponent component, ref StartCollideEvent args)
        {
            if (args.OurEntity != Entity)
                return;

            // only if ball
            if (EntityManager.TryGetComponent<BallComponent>(args.OtherEntity, out var ball))
            {
                VibrateAll(1, 250);
            }
        }

        private void OnPlayerAttached(PlayerAttachSysMessage ev)
        {
            if (!ev.AttachedEntity.Valid)
                return;

            Entity = ev.AttachedEntity;
        }
    }
}
