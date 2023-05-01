using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Numerics;
using System.ServiceModel.Channels;
using AssettoServer.Network.Packets.Incoming;
using AssettoServer.Network.Packets.Outgoing;
using AssettoServer.Network.Packets.Shared;
using AssettoServer.Network.Packets.UdpPlugin;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using CatMouseRacePlugin.Packets;
using Serilog;

namespace CatMouseRacePlugin;

public class EntryCarCatMouse
{
    private readonly SessionManager _sessionManager;
    private readonly EntryCarManager _entryCarManager;
    private readonly CatMouseChallengePlugin _plugin;
    private readonly EntryCar _entryCar;
    private readonly ACTcpClient? _acTcpClient;
    private readonly CatMouseRace.Factory _raceFactory;
    
    public int LightFlashCount { get; internal set; }
    
    internal CatMouseRace? CurrentRace { get; set; }
    internal Vector3 PitPosition { get; private set; }
    private int c =0;

    private long LastLightFlashTime { get; set; }
    private long LastRaceChallengeTime { get; set; }

    public EntryCarCatMouse(EntryCar entryCar, SessionManager sessionManager, EntryCarManager entryCarManager, CatMouseChallengePlugin plugin, CatMouseRace.Factory raceFactory)
    {
        _entryCar = entryCar;
        _acTcpClient = entryCar.Client;
        _sessionManager = sessionManager;
        _entryCarManager = entryCarManager;
        _plugin = plugin;
        _raceFactory = raceFactory;
        _entryCar.PositionUpdateReceived += OnPositionUpdateReceived;
        _entryCar.PositionUpdateReceived += SetUpPitPosition;
        _entryCarManager.ClientConnected += OnClientConnected;
    }

    private void Test(ACTcpClient sender, LapCompletedEventArgs e)
    {
        var packet = e.Packet;
        int c = 0;
        sender.SendPacket(new CatMouseRaceEvents
        {
            Index = sender.SessionId,
            Locked = 1,
            Rotation = sender.EntryCar.Status.Rotation

        });
        while (c < 3000)
        {
            c += 20;
            Thread.Sleep(20);
            sender.SendPacket(new CatMouseRaceEvents
            {
                Index = sender.SessionId,
                Locked = 2,
                Rotation = sender.EntryCar.Status.Rotation

            });
            Log.Debug($"{c}");
        }
        sender.SendPacket(new CatMouseRaceEvents
        {
            Index = sender.SessionId,
            Locked = 0,
            Rotation = sender.EntryCar.Status.Rotation

        });
        Log.Debug($"INDEX: {sender.SessionId}");
        Log.Information($"FULLY SIK BRUVSKI {packet.SessionId}, {packet.LapTime}, {sender.SessionId}, {sender.Name}");
    }

    private void Check(ACTcpClient sender, CollisionEventArgs e)
    {
        Log.Information($"Ye Mans crashed innit {e.TargetCar}, {e.Speed}");
    }

    private void OnClientConnected(ACTcpClient client, EventArgs args)
    {
        if (client.EntryCar.Equals(_entryCar))
        {
            Log.Debug($"Client {client.Name} connected, signing up client {client.EntryCar.SessionId} for new lap event");
            Log.Information($"Client {client.Name} current position: {client.EntryCar.Status.Position.X},{client.EntryCar.Status.Position.Y},{client.EntryCar.Status.Position.Z}");
            Log.Information($"Client {client.Name} current Rotation: {client.EntryCar.Status.GetRotationAngle()}");
            Log.Information($"Client {client.Name} current Velocity: {client.EntryCar.Status.GetVelocityAngle()}");
            client.LapCompleted += Test;
            client.Collision += Check;
        }
    }

    private bool IsInPits()
    {
        bool isInPits = true;

        float X = _entryCar.Status.Position.X;
        float Y = _entryCar.Status.Position.Y;
        float Z = _entryCar.Status.Position.Z;

        float pitX = PitPosition.X;
        float pitY = PitPosition.Y;
        float pitZ = PitPosition.Z;

        if (Math.Abs(X-pitX) >= 1 || Math.Abs(Y - pitY) >= 1 || Math.Abs(Z - pitZ) >= 1)
        {
            isInPits = false;
        }

        return isInPits;
    }

    private void SetUpPitPosition(EntryCar sender, in PositionUpdateIn positionUpdate)
    {
        bool settign = false;
        if (_entryCar.Status.Position.Length() > 0)
        {
            PitPosition = new Vector3
            {
                X = _entryCar.Status.Position.X,
                Y = _entryCar.Status.Position.Y,
                Z = _entryCar.Status.Position.Z
            };
            settign = true;

            _entryCar.PositionUpdateReceived -= SetUpPitPosition;
        }

        Log.Debug($"Pit Setup {settign}, {_entryCar.Status.Position.X}, {_entryCar.Status.Position.Y}, {_entryCar.Status.Position.Z}");
    }

    private void OnPositionUpdateReceived(EntryCar sender, in PositionUpdateIn positionUpdate)
    {
        c++;
        if (c == 19)
        {
            c = -1;
            double posX, posY, posZ, rotX, rotY, rotZ, absVel, scaledVel;
            posX = _entryCar.Status.Position.X;
            posY = _entryCar.Status.Position.Y;
            posZ = _entryCar.Status.Position.Z;

            rotX = _entryCar.Status.Velocity.X;
            rotY = _entryCar.Status.Velocity.Y;
            rotZ = _entryCar.Status.Velocity.Z;

            absVel = _entryCar.Status.Velocity.Length();

            scaledVel = (Vector3.Normalize(_entryCar.Status.Velocity) * 5).Length();

            float[] dmg = _entryCar.Status.DamageZoneLevel;
            Log.Debug($"POS: {posX.ToString("000.00")}, {posY.ToString("000.00")}, {posZ.ToString("000.00")} " +
                $"VEL: {rotX.ToString("000.00")}, {rotY.ToString("000.00")}, {rotZ.ToString("000.00")} " +
                $"SPEED: {absVel}, {scaledVel}");
        }
        long currentTick = _sessionManager.ServerTimeMilliseconds;
        if(((_entryCar.Status.StatusFlag & CarStatusFlags.LightsOn) == 0 && (positionUpdate.StatusFlag & CarStatusFlags.LightsOn) != 0) 
           || ((_entryCar.Status.StatusFlag & CarStatusFlags.HighBeamsOff) == 0 && (positionUpdate.StatusFlag & CarStatusFlags.HighBeamsOff) != 0))
        {
            LastLightFlashTime = currentTick;
            LightFlashCount++;
        }

        if ((_entryCar.Status.StatusFlag & CarStatusFlags.HazardsOn) == 0 
            && (positionUpdate.StatusFlag & CarStatusFlags.HazardsOn) != 0
            && CurrentRace != null 
            && CurrentRace.Challenged == sender 
            && !CurrentRace.HasStarted 
            && !CurrentRace.LineUpRequired)
        {
            _ = CurrentRace.StartAsync();
        }

        if (currentTick - LastLightFlashTime > 3000 && LightFlashCount > 0)
        {
            LightFlashCount = 0;
        }

        if (LightFlashCount == 3)
        {
            LightFlashCount = 0;

            if(currentTick - LastRaceChallengeTime > 20000)
            {
                Task.Run(ChallengeNearbyCar);
                LastRaceChallengeTime = currentTick;
            }
        }
    }

    internal void ChallengeCar(EntryCar car, bool lineUpRequired = true)
    {
        void Reply(string message)
            => _entryCar.Client?.SendPacket(new ChatMessage { SessionId = 255, Message = message });

        var currentRace = CurrentRace;
        if (currentRace != null)
        {
            if (currentRace.HasStarted)
                Reply("You are currently in a race.");
            else
                Reply("You have a pending race request.");
        }
        else
        {
            if (car == _entryCar)
                Reply("You cannot challenge yourself to a race.");
            else
            {
                currentRace = _plugin.GetRace(car).CurrentRace;
                if (currentRace != null)
                {
                    if (currentRace.HasStarted)
                        Reply("This car is currently in a race.");
                    else
                        Reply("This car has a pending race request.");
                }
                else
                {
                    currentRace = _raceFactory(_entryCar, car, lineUpRequired);
                    CurrentRace = currentRace;
                    _plugin.GetRace(car).CurrentRace = currentRace;

                    _entryCar.Client?.SendPacket(new ChatMessage { SessionId = 255, Message = $"You have challenged {car.Client?.Name} to a race." });

                    if (lineUpRequired)
                        car.Client?.SendPacket(new ChatMessage { SessionId = 255, Message = $"{_entryCar.Client?.Name} has challenged you to a race. Send /accept within 10 seconds to accept." });
                    else
                        car.Client?.SendPacket(new ChatMessage
                            { SessionId = 255, Message = $"{_entryCar.Client?.Name} has challenged you to a race. Flash your hazard lights or send /accept within 10 seconds to accept." });

                    _ = Task.Delay(10000).ContinueWith(t =>
                    {
                        if (!currentRace.HasStarted)
                        {
                            CurrentRace = null;
                            _plugin.GetRace(car).CurrentRace = null;

                            ChatMessage timeoutMessage = new ChatMessage { SessionId = 255, Message = "Race request has timed out." };
                            _entryCar.Client?.SendPacket(timeoutMessage);
                            car.Client?.SendPacket(timeoutMessage);
                        }
                    });
                }
            }
        }
    }

    //Challenge a car within some distance and in between 
    private void ChallengeNearbyCar()
    {
        EntryCar? bestMatch = null;
        const float distanceSquared = 30 * 30;

        foreach(EntryCar car in _entryCarManager.EntryCars)
        {
            ACTcpClient? carClient = car.Client;
            if(carClient != null && car != _entryCar)
            {
                float challengedAngle = (float)(Math.Atan2(_entryCar.Status.Position.X - car.Status.Position.X, _entryCar.Status.Position.Z - car.Status.Position.Z) * 180 / Math.PI);
                if (challengedAngle < 0)
                    challengedAngle += 360;
                float challengedRot = car.Status.GetRotationAngle();

                challengedAngle += challengedRot;
                challengedAngle %= 360;

                if (challengedAngle > 110 && challengedAngle < 250 && Vector3.DistanceSquared(car.Status.Position, _entryCar.Status.Position) < distanceSquared)
                    bestMatch = car;
            }
        }

        if (bestMatch != null)
            ChallengeCar(bestMatch, false);
    }
}
