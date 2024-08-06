namespace VoxelPlay {

    // Fallback terrain generator for empty worlds
    public class NullTerrainGenerator : VoxelPlayTerrainGenerator {

        public override bool PaintChunk(VoxelChunk chunk) {
            return false;
        }

    }

}