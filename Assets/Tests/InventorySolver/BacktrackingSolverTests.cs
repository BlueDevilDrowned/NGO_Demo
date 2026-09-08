using System.Collections.Generic;
using NUnit.Framework;

namespace InventorySolver.Tests
{
    public sealed class BacktrackingSolverTests
    {
        private static InventoryLayout Layout(int width, int height)
        {
            var valid = new List<bool>();
            for (int i = 0; i < width * height; i++) valid.Add(true);
            return new InventoryLayout(new[] { new RegionLayout(width, height, valid) });
        }

        private static SolverItem Item(int token, Cell[] cells, Placement? placement = null, bool canMove = true)
        {
            return new SolverItem(token, ShapeRotator.Build(new Shape(cells), true), placement, canMove);
        }

        [Test]
        public void PlacesTargetInEmptyCell()
        {
            var layout = Layout(3, 1);
            var existing = Item(1, new[] { new Cell(0, 0) }, new Placement(0, new Cell(0, 0), ERotation.R0), false);
            var target = Item(2, new[] { new Cell(0, 0) });

            SolveResult result = new BacktrackingSolver().TryAutoPlace(layout, new[] { existing }, target, new SolverOptions());

            Assert.IsTrue(result.Success);
            bool found = false;
            foreach (SolvedItem solved in result.Items)
                if (solved.Token == target.Token && solved.IsTarget) found = true;
            Assert.IsTrue(found);
        }

        [Test]
        public void CanPlaceRejectsOverlap()
        {
            var layout = Layout(2, 1);
            var existing = Item(1, new[] { new Cell(0, 0) }, new Placement(0, new Cell(0, 0), ERotation.R0), false);
            var target = Item(2, new[] { new Cell(0, 0) });

            PlacementCheckResult check = new BacktrackingSolver().CanPlace(layout, new[] { existing }, target,
                new Placement(0, new Cell(0, 0), ERotation.R0));

            Assert.IsFalse(check.CanPlace);
            Assert.AreEqual(PlacementFailureReason.Occupied, check.Reason);
        }
    }
}
