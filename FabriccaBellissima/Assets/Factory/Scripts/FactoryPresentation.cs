using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabriccaBellissima.Factory
{
    public sealed class FactoryPresentation : MonoBehaviour
    {
        [SerializeField] private FactorySimulationCoordinator _coordinator;
        [SerializeField] private bool _showWorldLabels = true;

        private readonly Dictionary<int, FactoryNodeConfig> _configs = new Dictionary<int, FactoryNodeConfig>();
        private readonly Dictionary<int, TextMesh> _labels = new Dictionary<int, TextMesh>();
        private readonly Dictionary<int, Renderer> _machineRenderers = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, Renderer> _beaconRenderers = new Dictionary<int, Renderer>();
        private readonly Dictionary<long, GameObject> _itemObjects = new Dictionary<long, GameObject>();
        private readonly Dictionary<long, FactoryResourceType> _itemTypes = new Dictionary<long, FactoryResourceType>();
        private Transform _visualRoot;
        private Material _machineMaterial;
        private Material _beltMaterial;

        private void Start()
        {
            if (_coordinator == null) _coordinator = GetComponent<FactorySimulationCoordinator>();
            if (_coordinator == null || _coordinator.Simulation == null)
            {
                enabled = false;
                return;
            }

            BuildPresentation();
            SyncPresentation();
        }

        private void LateUpdate()
        {
            if (_coordinator?.Simulation != null) SyncPresentation();
        }

        private void BuildPresentation()
        {
            _visualRoot = new GameObject("Runtime Factory Presentation (non-authoritative)").transform;
            _visualRoot.SetParent(transform, false);
            _machineMaterial = CreateMaterial("Machine", new Color(0.18f, 0.28f, 0.35f));
            _beltMaterial = CreateMaterial("Belt", new Color(0.08f, 0.1f, 0.12f));

            foreach (FactoryNodeConfig config in _coordinator.Simulation.NodeConfigs)
            {
                _configs.Add(config.nodeId, config);
                if (config.kind == FactoryNodeKind.Conveyor) CreateBelt(config);
                else CreateMachine(config);
                if (_showWorldLabels) CreateLabel(config);
            }
        }

        private void CreateMachine(FactoryNodeConfig config)
        {
            GameObject machine = GameObject.CreatePrimitive(config.kind == FactoryNodeKind.Sink
                ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            machine.name = config.displayName;
            machine.transform.SetParent(_visualRoot, false);
            machine.transform.position = config.position;
            machine.transform.localScale = config.kind == FactoryNodeKind.Sink
                ? new Vector3(1.4f, 0.7f, 1.4f)
                : new Vector3(2.2f, 1.4f, 2.2f);
            Renderer renderer = machine.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(_machineMaterial);
            _machineRenderers.Add(config.nodeId, renderer);

            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Activity Beacon";
            beacon.transform.SetParent(machine.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            beacon.transform.localScale = new Vector3(0.22f, 0.18f, 0.22f);
            Renderer beaconRenderer = beacon.GetComponent<Renderer>();
            beaconRenderer.sharedMaterial = CreateMaterial("Beacon", new Color(0.1f, 0.8f, 0.85f));
            _beaconRenderers.Add(config.nodeId, beaconRenderer);
        }

        private void CreateBelt(FactoryNodeConfig config)
        {
            Vector3 delta = config.beltEnd - config.beltStart;
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = config.displayName;
            belt.transform.SetParent(_visualRoot, false);
            belt.transform.position = (config.beltStart + config.beltEnd) * 0.5f;
            belt.transform.localScale = new Vector3(0.9f, 0.18f, delta.magnitude);
            belt.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            belt.GetComponent<Renderer>().sharedMaterial = _beltMaterial;

            for (int index = 1; index <= 3; index++)
            {
                float t = index / 4f;
                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = "Direction marker";
                arrow.transform.SetParent(_visualRoot, false);
                arrow.transform.position = Vector3.Lerp(config.beltStart, config.beltEnd, t) + Vector3.up * 0.16f;
                arrow.transform.localScale = new Vector3(0.45f, 0.05f, 0.12f);
                arrow.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                arrow.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Arrow", new Color(1f, 0.65f, 0.08f));
            }
        }

        private void CreateLabel(FactoryNodeConfig config)
        {
            var labelObject = new GameObject(config.displayName + " Label");
            labelObject.transform.SetParent(_visualRoot, false);
            labelObject.transform.position = config.position + Vector3.up *
                (config.kind == FactoryNodeKind.Conveyor ? 1.2f : 2f);
            labelObject.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 36;
            text.characterSize = 0.08f;
            text.color = Color.white;
            text.text = config.displayName + "\n" + PortLegend(config.kind);
            _labels.Add(config.nodeId, text);
        }

        private void SyncPresentation()
        {
            FactorySnapshot snapshot = _coordinator.CurrentSnapshot;
            if (snapshot == null) return;
            var visibleIds = new HashSet<long>();

            foreach (FactoryNodeSnapshot node in snapshot.nodes)
            {
                FactoryNodeConfig config = _configs[node.nodeId];
                if (_labels.TryGetValue(node.nodeId, out TextMesh label))
                    label.text = BuildWorldLabel(node);
                if (_machineRenderers.TryGetValue(node.nodeId, out Renderer renderer))
                    renderer.sharedMaterial.color = node.enabled
                        ? (node.status.Contains("blocked") ? new Color(0.65f, 0.22f, 0.12f) : new Color(0.18f, 0.38f, 0.42f))
                        : new Color(0.14f, 0.14f, 0.14f);
                if (_beaconRenderers.TryGetValue(node.nodeId, out Renderer beacon))
                {
                    bool active = node.status == "Extracting" || node.status == "Smelting" ||
                                  node.status == "Spraying";
                    float pulse = active ? 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f) : 0.2f;
                    beacon.sharedMaterial.color = node.enabled
                        ? Color.Lerp(new Color(0.02f, 0.12f, 0.14f), new Color(0.1f, 1f, 0.85f), pulse)
                        : Color.black;
                }

                if (node.outputItem != null)
                    PlaceItem(node.outputItem, config.position + new Vector3(1.2f, 1.05f, 0f), node, visibleIds);
                if (node.workItem != null)
                    PlaceItem(node.workItem, config.position + new Vector3(0f, 1.2f, 0f), node, visibleIds);
                if (node.waitingCoal != null)
                    PlaceItem(node.waitingCoal, config.position + new Vector3(-0.65f, 1.05f, -0.65f), node, visibleIds);
                if (node.waitingCan != null)
                    PlaceItem(node.waitingCan, config.position + new Vector3(-0.65f, 1.05f, 0.65f), node, visibleIds);
                foreach (FactoryBeltItem beltItem in node.beltItems)
                {
                    float progress = node.lengthUnits == 0 ? 0f : beltItem.progressUnits / (float)node.lengthUnits;
                    Vector3 position = Vector3.Lerp(config.beltStart, config.beltEnd, progress) + Vector3.up * 0.45f;
                    PlaceItem(beltItem.item, position, node, visibleIds);
                }
            }

            foreach (long id in _itemObjects.Keys.Where(id => !visibleIds.Contains(id)).ToArray())
            {
                Destroy(_itemObjects[id]);
                _itemObjects.Remove(id);
                _itemTypes.Remove(id);
            }
        }

        private void PlaceItem(FactoryItem item, Vector3 position, FactoryNodeSnapshot owner, HashSet<long> visibleIds)
        {
            visibleIds.Add(item.id);
            if (_itemObjects.TryGetValue(item.id, out GameObject existing) && _itemTypes[item.id] != item.resource)
            {
                Destroy(existing);
                _itemObjects.Remove(item.id);
                _itemTypes.Remove(item.id);
            }

            if (!_itemObjects.TryGetValue(item.id, out GameObject visual))
            {
                visual = CreateItemVisual(item);
                _itemObjects.Add(item.id, visual);
                _itemTypes.Add(item.id, item.resource);
            }

            visual.transform.position = position;
            Color color = ResourceColor(item);
            if (item.resource == FactoryResourceType.IronBar && owner.kind == FactoryNodeKind.ColoringStation &&
                owner.chargesPerBar > 0)
                color = Color.Lerp(color, ColorForId(owner.configuredColorId),
                    owner.appliedCharges / (float)owner.chargesPerBar);
            visual.GetComponent<Renderer>().sharedMaterial.color = color;
        }

        private GameObject CreateItemVisual(FactoryItem item)
        {
            PrimitiveType primitive = item.resource == FactoryResourceType.IronOre ? PrimitiveType.Sphere :
                item.resource == FactoryResourceType.PaintCan ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.name = $"{item.resource} #{item.id}";
            visual.transform.SetParent(_visualRoot, false);
            switch (item.resource)
            {
                case FactoryResourceType.IronOre:
                    visual.transform.localScale = Vector3.one * 0.52f;
                    break;
                case FactoryResourceType.Coal:
                    visual.transform.localScale = Vector3.one * 0.46f;
                    visual.transform.rotation = Quaternion.Euler(20f, 30f, 10f);
                    break;
                case FactoryResourceType.PaintCan:
                    visual.transform.localScale = new Vector3(0.38f, 0.32f, 0.38f);
                    break;
                default:
                    visual.transform.localScale = new Vector3(0.9f, 0.24f, 0.38f);
                    break;
            }
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Item " + item.id, ResourceColor(item));
            return visual;
        }

        private static string BuildWorldLabel(FactoryNodeSnapshot node)
        {
            string detail;
            switch (node.kind)
            {
                case FactoryNodeKind.Conveyor: detail = $"{node.beltItems.Count}/{node.capacity}"; break;
                case FactoryNodeKind.Furnace: detail = $"work {node.workProgress}/{node.workRequired} | fuel {node.fuelRemaining}"; break;
                case FactoryNodeKind.ColoringStation: detail = $"paint {node.appliedCharges}/{node.chargesPerBar} | tank {node.reservoirCharges}"; break;
                case FactoryNodeKind.Sink: detail = $"total {node.sinkTotal}"; break;
                default: detail = $"work {node.workProgress}/{node.workRequired}"; break;
            }
            return $"{node.displayName}\n{PortLegend(node.kind)}\n{node.status} | {detail}";
        }

        private static string PortLegend(FactoryNodeKind kind)
        {
            switch (kind)
            {
                case FactoryNodeKind.Extractor: return "OUTPUT →";
                case FactoryNodeKind.Conveyor: return "IN → OUT";
                case FactoryNodeKind.Furnace: return "ORE + COAL → BAR";
                case FactoryNodeKind.ColoringStation: return "BAR + CAN → PAINTED";
                case FactoryNodeKind.Sink: return "PAINTED IN";
                default: return string.Empty;
            }
        }

        private static Color ResourceColor(FactoryItem item)
        {
            switch (item.resource)
            {
                case FactoryResourceType.IronOre: return new Color(0.68f, 0.28f, 0.13f);
                case FactoryResourceType.Coal: return new Color(0.04f, 0.04f, 0.05f);
                case FactoryResourceType.PaintCan: return ColorForId(item.colorId);
                case FactoryResourceType.IronBar: return new Color(0.7f, 0.74f, 0.78f);
                case FactoryResourceType.PaintedIronBar: return ColorForId(item.colorId);
                default: return Color.magenta;
            }
        }

        private static Color ColorForId(string colorId)
        {
            if (string.Equals(colorId, "Blue", System.StringComparison.OrdinalIgnoreCase))
                return new Color(0.08f, 0.35f, 1f);
            return new Color(0.7f, 0.2f, 0.8f);
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = materialName, color = color };
            return material;
        }
    }
}
