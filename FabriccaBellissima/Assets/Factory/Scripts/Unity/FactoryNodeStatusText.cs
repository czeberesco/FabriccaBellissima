namespace FabriccaBellissima.Factory
{
    public static class FactoryNodeStatusText
    {
        public static string Format(FactoryNodeSnapshot node)
        {
            if (node.kind == FactoryNodeKind.Extractor)
                return $"{node.status} | work {node.workProgress}/{node.workRequired} | " +
                       $"output {(node.outputItem == null ? "empty" : node.outputItem.resource + " #" + node.outputItem.id)}";
            if (node.kind == FactoryNodeKind.Conveyor)
                return $"{node.status} | capacity {node.beltItems.Count}/{node.capacity}";
            if (node.kind == FactoryNodeKind.Furnace)
                return $"{node.status} | smelt {node.workProgress}/{node.workRequired} | fuel {node.fuelRemaining} | " +
                       $"queued coal {(node.waitingCoal == null ? "no" : "#" + node.waitingCoal.id)}";
            if (node.kind == FactoryNodeKind.ColoringStation)
                return $"{node.status} | bar {node.appliedCharges}/{node.chargesPerBar} | spray " +
                       $"{node.sprayProgress}/{node.sprayTicks} | reservoir {node.reservoirColorId} " +
                       $"{node.reservoirCharges} | waiting can " +
                       (node.waitingCan == null ? "no" : "#" + node.waitingCan.id);
            if (node.kind == FactoryNodeKind.Sink)
                return $"{node.status} | last ID " +
                       (node.consumedItemIds.Count == 0 ? "none" : node.consumedItemIds[node.consumedItemIds.Count - 1].ToString());
            return node.status;
        }
    }
}
