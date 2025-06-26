using Assets.KaomoLab.CSEmulator;
using Assets.KaomoLab.CSEmulator.Components;
using Assets.KaomoLab.CSEmulator.Editor.Engine;
using UnityEngine;
using ILogger = Assets.KaomoLab.CSEmulator.Editor.ILogger;

namespace Silksprite.ClusterScriptLogConsoleWindow2.Interop.CSEmulator
{
    public class ScriptableItemLogExtLoggerFactory : ILoggerFactory
    {
        readonly IPlayerMeta _playerMeta;

        public ScriptableItemLogExtLoggerFactory(IPlayerMeta playerMeta)
        {
            _playerMeta = playerMeta;
        }

        public ILogger Create(GameObject gameObject, IProgramStatus programStatus)
        {
            return new ScriptableItemLogExtLogger(gameObject, _playerMeta, programStatus);
        }
    }
}
