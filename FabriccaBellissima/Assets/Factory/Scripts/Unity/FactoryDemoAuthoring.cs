using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryDemoAuthoring : MonoBehaviour
    {
        public const int DistanceUnitsPerWorldUnit = 1000;

        [Header("Simulation")]
        [SerializeField, Min(0.001f)] private float _tickDurationSeconds = 0.05f;
        [SerializeField] private string _paintColorId = "Blue";

        [Header("Durations (seconds)")]
        [SerializeField, Min(0.001f)] private float _oreExtractionSeconds = 2f;
        [SerializeField, Min(0.001f)] private float _coalExtractionSeconds = 6f;
        [SerializeField, Min(0.001f)] private float _paintExtractionSeconds = 4f;
        [SerializeField, Min(0.001f)] private float _coalBurnSeconds = 8f;
        [SerializeField, Min(0.001f)] private float _furnaceProcessingSeconds = 2f;
        [SerializeField, Min(0.001f)] private float _spraySecondsPerCharge = 0.75f;

        [Header("Paint")]
        [SerializeField, Min(1)] private int _chargesPerCan = 3;
        [SerializeField, Min(1)] private int _chargesPerBar = 2;

        [Header("Conveyors")]
        [SerializeField, Min(0.001f)] private float _beltLengthWorldUnits = 4f;
        [SerializeField, Min(0.001f)] private float _beltSpeedWorldUnitsPerSecond = 1f;
        [SerializeField, Min(1)] private int _beltCapacity = 4;

        [Header("Stable topology and editable layout")]
        [SerializeField] private List<FactoryNodeLayout> _nodeLayouts = new List<FactoryNodeLayout>();
        [SerializeField] private List<FactoryConnection> _connections = new List<FactoryConnection>();

        public float TickDurationSeconds => _tickDurationSeconds;
        public IReadOnlyList<FactoryNodeLayout> NodeLayouts => _nodeLayouts;
        public IReadOnlyList<FactoryConnection> Connections => _connections;

        public void ResetToDefaults()
        {
            _tickDurationSeconds = 0.05f;
            _paintColorId = "Blue";
            _oreExtractionSeconds = 2f;
            _coalExtractionSeconds = 6f;
            _paintExtractionSeconds = 4f;
            _coalBurnSeconds = 8f;
            _furnaceProcessingSeconds = 2f;
            _spraySecondsPerCharge = 0.75f;
            _chargesPerCan = 3;
            _chargesPerBar = 2;
            _beltLengthWorldUnits = 4f;
            _beltSpeedWorldUnitsPerSecond = 1f;
            _beltCapacity = 4;

            _nodeLayouts = new List<FactoryNodeLayout>
            {
                Layout(10, new Vector3(-9f, 0.5f, 4f)),
                BeltLayout(20, new Vector3(-7f, 0.4f, 4f), new Vector3(-3f, 0.4f, 4f)),
                Layout(30, new Vector3(-9f, 0.5f, 0f)),
                BeltLayout(40, new Vector3(-7f, 0.4f, 0f), new Vector3(-3f, 0.4f, 0f)),
                Layout(50, new Vector3(-1f, 0.7f, 2f)),
                BeltLayout(60, new Vector3(1f, 0.4f, 2f), new Vector3(5f, 0.4f, 2f)),
                Layout(70, new Vector3(1f, 0.5f, -3f)),
                BeltLayout(80, new Vector3(3f, 0.4f, -3f), new Vector3(7f, 0.4f, -3f)),
                Layout(90, new Vector3(7f, 0.7f, 1f)),
                BeltLayout(100, new Vector3(9f, 0.4f, 1f), new Vector3(13f, 0.4f, 1f)),
                Layout(110, new Vector3(15f, 0.6f, 1f))
            };

            _connections = new List<FactoryConnection>
            {
                Connect(10, FactoryPorts.Output, 20, FactoryPorts.Input),
                Connect(20, FactoryPorts.Output, 50, FactoryPorts.FurnaceOre),
                Connect(30, FactoryPorts.Output, 40, FactoryPorts.Input),
                Connect(40, FactoryPorts.Output, 50, FactoryPorts.FurnaceFuel),
                Connect(50, FactoryPorts.FurnaceOutput, 60, FactoryPorts.Input),
                Connect(60, FactoryPorts.Output, 90, FactoryPorts.ColoringBar),
                Connect(70, FactoryPorts.Output, 80, FactoryPorts.Input),
                Connect(80, FactoryPorts.Output, 90, FactoryPorts.ColoringCan),
                Connect(90, FactoryPorts.ColoringOutput, 100, FactoryPorts.Input),
                Connect(100, FactoryPorts.Output, 110, FactoryPorts.Input)
            };
        }

        public FactoryScenario CompileScenario()
        {
            ValidateAuthoring();
            int beltLength = ConvertLength(_beltLengthWorldUnits);
            int movement = ConvertMovement(_beltSpeedWorldUnitsPerSecond, _tickDurationSeconds);
            int spacing = Math.Max(1, beltLength / _beltCapacity);

            var scenario = new FactoryScenario
            {
                tickDurationMicroseconds = checked((int)Math.Round(
                    (decimal)_tickDurationSeconds * 1000000m, MidpointRounding.AwayFromZero)),
                nextItemId = 1
            };

            scenario.nodes.Add(Extractor(10, "IRON EXTRACTOR", FactoryResourceType.IronOre,
                ToTicks(_oreExtractionSeconds), 0));
            scenario.nodes.Add(Belt(20, "ORE BELT", beltLength, movement, spacing));
            scenario.nodes.Add(Extractor(30, "COAL EXTRACTOR", FactoryResourceType.Coal,
                ToTicks(_coalExtractionSeconds), 0));
            scenario.nodes.Add(Belt(40, "COAL BELT", beltLength, movement, spacing));
            scenario.nodes.Add(new FactoryNodeConfig
            {
                nodeId = 50, displayName = "FURNACE", kind = FactoryNodeKind.Furnace, enabled = true,
                burnTicks = ToTicks(_coalBurnSeconds), smeltTicks = ToTicks(_furnaceProcessingSeconds)
            });
            scenario.nodes.Add(Belt(60, "BAR BELT", beltLength, movement, spacing));
            scenario.nodes.Add(Extractor(70, "PAINT EXTRACTOR", FactoryResourceType.PaintCan,
                ToTicks(_paintExtractionSeconds), _chargesPerCan));
            scenario.nodes.Add(Belt(80, "PAINT BELT", beltLength, movement, spacing));
            scenario.nodes.Add(new FactoryNodeConfig
            {
                nodeId = 90, displayName = "COLORING STATION", kind = FactoryNodeKind.ColoringStation,
                enabled = true, colorId = _paintColorId, sprayTicks = ToTicks(_spraySecondsPerCharge),
                chargesPerBar = _chargesPerBar
            });
            scenario.nodes.Add(Belt(100, "OUTPUT BELT", beltLength, movement, spacing));
            scenario.nodes.Add(new FactoryNodeConfig
            {
                nodeId = 110, displayName = "TERMINATION SINK", kind = FactoryNodeKind.Sink, enabled = true
            });

            var layoutById = _nodeLayouts.ToDictionary(layout => layout.nodeId);
            foreach (FactoryNodeConfig node in scenario.nodes)
            {
                FactoryNodeLayout layout = layoutById[node.nodeId];
                node.position = layout.position;
                node.beltStart = layout.beltStart;
                node.beltEnd = layout.beltEnd;
            }

            scenario.connections.AddRange(_connections.Select(connection => new FactoryConnection
            {
                sourceNodeId = connection.sourceNodeId,
                sourcePortId = connection.sourcePortId,
                destinationNodeId = connection.destinationNodeId,
                destinationPortId = connection.destinationPortId
            }));
            return scenario;
        }

        private FactoryNodeConfig Extractor(int id, string displayName, FactoryResourceType resource,
            int duration, int canCharges)
        {
            return new FactoryNodeConfig
            {
                nodeId = id, displayName = displayName, kind = FactoryNodeKind.Extractor, enabled = true,
                resource = resource, durationTicks = duration, colorId = _paintColorId,
                paintCanCharges = canCharges
            };
        }

        private FactoryNodeConfig Belt(int id, string displayName, int length, int movement, int spacing)
        {
            return new FactoryNodeConfig
            {
                nodeId = id, displayName = displayName, kind = FactoryNodeKind.Conveyor, enabled = true,
                lengthUnits = length, movementPerTick = movement, capacity = _beltCapacity,
                spacingUnits = spacing
            };
        }

        // Decimal conversion removes binary-float noise before the contractually required ceiling.
        // For example, 1 unit/s * 0.05 s * 1000 must author exactly 50, not 50.000004 -> 51.
        private int ToTicks(float seconds) => Math.Max(1,
            checked((int)Math.Ceiling((decimal)seconds / (decimal)_tickDurationSeconds)));
        private static int ConvertLength(float worldUnits) => Math.Max(1,
            checked((int)Math.Ceiling((decimal)worldUnits * DistanceUnitsPerWorldUnit)));
        private static int ConvertMovement(float speed, float tick) => Math.Max(1,
            checked((int)Math.Ceiling((decimal)speed * (decimal)tick * DistanceUnitsPerWorldUnit)));

        private void ValidateAuthoring()
        {
            if (!IsPositiveFinite(_tickDurationSeconds) || !IsPositiveFinite(_oreExtractionSeconds) ||
                !IsPositiveFinite(_coalExtractionSeconds) || !IsPositiveFinite(_paintExtractionSeconds) ||
                !IsPositiveFinite(_coalBurnSeconds) || !IsPositiveFinite(_furnaceProcessingSeconds) ||
                !IsPositiveFinite(_spraySecondsPerCharge) || !IsPositiveFinite(_beltLengthWorldUnits) ||
                !IsPositiveFinite(_beltSpeedWorldUnitsPerSecond))
                throw new InvalidOperationException("All durations, belt length, and belt speed must be finite and positive.");
            if (_chargesPerCan <= 0 || _chargesPerBar <= 0 || _beltCapacity <= 0)
                throw new InvalidOperationException("Charges and belt capacity must be positive.");
            if (string.IsNullOrWhiteSpace(_paintColorId))
                throw new InvalidOperationException("Paint color ID cannot be empty.");
            int[] expectedIds = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110 };
            if (_nodeLayouts == null || _nodeLayouts.Count != expectedIds.Length ||
                !_nodeLayouts.Select(layout => layout.nodeId).OrderBy(id => id).SequenceEqual(expectedIds))
                throw new InvalidOperationException("The demo requires exactly one unique layout for every stable node ID.");
            if (_connections == null || _connections.Count != 10)
                throw new InvalidOperationException("The demo requires its ten explicit port connections.");
        }

        private static bool IsPositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        private static FactoryNodeLayout Layout(int id, Vector3 position) =>
            new FactoryNodeLayout { nodeId = id, position = position, beltStart = position, beltEnd = position };
        private static FactoryNodeLayout BeltLayout(int id, Vector3 start, Vector3 end) =>
            new FactoryNodeLayout { nodeId = id, position = (start + end) * 0.5f, beltStart = start, beltEnd = end };
        private static FactoryConnection Connect(int source, int sourcePort, int destination, int destinationPort) =>
            new FactoryConnection { sourceNodeId = source, sourcePortId = sourcePort,
                destinationNodeId = destination, destinationPortId = destinationPort };

        private void Reset() => ResetToDefaults();
    }
}
