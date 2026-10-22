using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    public enum CardShapeSource
    {
        Preset,
        Polygon,
        Sprite
    }

    public enum CardShapePreset
    {
        Rectangle,
        Ellipse,
        RegularPolygon
    }

    public enum CardSpriteUsage
    {
        Visual,
        Mask
    }
    //Microclass to determine card shape
    [Serializable]
    public class CardShapeSettings
    {
        public CardShapeSource source = CardShapeSource.Preset;
        public CardShapePreset preset = CardShapePreset.Rectangle;

        // Card size in world units
        [Min(0.05f)] public Vector2 size = new Vector2(2.5f, 3.5f);

        [Min(0f)] public float cornerRadius = 0.15f;
        [Range(1, 16)] public int cornerSegments = 6;

        [Range(3, 12)] public int polygonSides = 6;
        public float polygonRotation;
        [Range(8, 128)] public int ellipseSegments = 64;

        //Polygon source points, normalized to card space (-0.5/0.5)
        public List<Vector2> points = new List<Vector2>();

        public Sprite sprite;
        public CardSpriteUsage spriteUsage = CardSpriteUsage.Visual;
    }
}
