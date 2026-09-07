using System;

namespace InventorySolver
{
    public sealed class SolverItem
    {
        /// <summary>
        /// 计算时给物品加的临时编号。不干扰外部id映射
        /// </summary>
        public int Token { get; }
        public ShapeRotationSet Rotations { get; }
        public Placement? CurrentPlacement { get; }
        public bool CanMove { get; }

        public SolverItem(int token, ShapeRotationSet rotations, Placement? currentPlacement, bool canMove)
        {
            if(rotations==null) throw new ArgumentNullException(nameof(rotations));
            Token=token; Rotations=rotations; CurrentPlacement=currentPlacement; CanMove=canMove;
        }
    }
}
