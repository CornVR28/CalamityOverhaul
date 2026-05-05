using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core
{
    /// <summary>
    /// 史莱姆王状态机，服务端/单人端驱动状态转移，客户端通过 npc.ai[2] 同步
    /// </summary>
    internal class KingSlimeStateMachine
    {
        public IKingSlimeState CurrentState { get; private set; }
        public IKingSlimeState PreviousState { get; private set; }
        public KingSlimeStateContext Context { get; private set; }

        public KingSlimeStateMachine(KingSlimeStateContext context) {
            Context = context;
        }

        public void SetInitialState(IKingSlimeState state) {
            CurrentState = state;
            CurrentState?.OnEnter(Context);
            SyncStateToAI();
        }

        public void ForceChangeState(IKingSlimeState newState) {
            if (newState == null) return;
            CurrentState?.OnExit(Context);
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.OnEnter(Context);
            SyncStateToAI();
            Context.Npc.netUpdate = true;
        }

        public void Update() {
            if (CurrentState == null) return;

            if (VaultUtils.isClient) {
                SyncStateFromAI();
            }

            IKingSlimeState nextState = CurrentState.OnUpdate(Context);

            if (!VaultUtils.isClient && nextState != null && nextState != CurrentState) {
                CurrentState.OnExit(Context);
                PreviousState = CurrentState;
                CurrentState = nextState;
                CurrentState.OnEnter(Context);
                SyncStateToAI();
                Context.Npc.netUpdate = true;
            }
        }

        private void SyncStateToAI() {
            if (CurrentState != null && Context.Npc != null) {
                Context.Npc.ai[2] = (float)CurrentState.StateIndex;
            }
        }

        private void SyncStateFromAI() {
            if (Context.Npc == null || CurrentState == null) return;
            int serverStateIndex = (int)Context.Npc.ai[2];
            if (serverStateIndex != (int)CurrentState.StateIndex) {
                IKingSlimeState newState = CreateStateFromIndex((KingSlimeStateIndex)serverStateIndex);
                if (newState != null) {
                    CurrentState.OnExit(Context);
                    PreviousState = CurrentState;
                    CurrentState = newState;
                    CurrentState.OnEnter(Context);
                }
            }
        }

        internal static IKingSlimeState CreateStateFromIndex(KingSlimeStateIndex index) {
            return index switch {
                KingSlimeStateIndex.Intro => new KingSlimeIntroState(),
                KingSlimeStateIndex.Hop => new KingSlimeHopState(),
                KingSlimeStateIndex.RoyalSlamPrepare => new KingSlimeRoyalSlamPrepareState(),
                KingSlimeStateIndex.RoyalSlamFalling => new KingSlimeRoyalSlamFallingState(),
                KingSlimeStateIndex.CrownBarrage => new KingSlimeCrownBarrageState(),
                KingSlimeStateIndex.SlimeRain => new KingSlimeSlimeRainState(),
                KingSlimeStateIndex.TeleDash => new KingSlimeTeleDashState(),
                KingSlimeStateIndex.Despawn => new KingSlimeDespawnState(),
                _ => null,
            };
        }
    }
}
