using NUnit.Framework;
using UnityEngine;

namespace CinematicSequencer.Animation.Tests
{
    // TODO: 再レビュー
    public class AnimationCurveEvaluationTest
    {
        const float Epsilon = 0.000001f;

        //--------------------------------------------------------------------
        // 1) シンプルな 2 キーテスト
        //--------------------------------------------------------------------
        [Test]
        public void TwoKeys_LinearRamp_MatchUnity()
        {
            var custom = new AnimationCurve();
            custom.AddKey(new Keyframe(time: 0f, value: 0f));
            custom.AddKey(new Keyframe(time: 1f, value: 1f));

            var unity = new UnityEngine.AnimationCurve();
            unity.AddKey(new UnityEngine.Keyframe(time: 0f, value: 0f));
            unity.AddKey(new UnityEngine.Keyframe(time: 1f, value: 1f));

            for (var i = 0; i <= 1000; ++i)
            {
                var t = i / 1000f;

                // var logText = $"t = {t:F3}, custom = {custom.Evaluate(t)}, unity = {unity.Evaluate(t)}";
                // Debug.Log($"[{nameof(AnimationCurveEvaluationTest)}] {logText}");

                Assert.That(custom.Evaluate(t),
                            Is.EqualTo(unity.Evaluate(t)).Within(Epsilon),
                            $"Mismatch at t = {t:F3}");
            }
        }

        //--------------------------------------------------------------------
        // 2) ランダム生成テスト ― さまざまな曲線・サンプル点で比較
        //--------------------------------------------------------------------
        [Test]
        public void RandomizedCurves_MatchUnity()
        {
            var rng = new System.Random(20250428);

            for (int trial = 0; trial < 50; ++trial)
            {
                // --- ランダムに 2〜10 キーを生成（単調増加時間を保証） ---
                int keyCount = rng.Next(2, 10);

                var custom = new AnimationCurve();
                var unity  = new UnityEngine.AnimationCurve();

                for (int k = 0; k < keyCount; ++k)
                {
                    var time  = k;                                 // 0,1,2,… 単純化
                    var value = (float)(rng.NextDouble() * 2 - 1); // [-1,1]

                    custom.AddKey(new Keyframe(time, value));
                    unity.AddKey(new UnityEngine.Keyframe(time, value));
                }

                // --- 任意のサンプル点で比較 ---
                for (int i = 0; i < 100; ++i)
                {
                    var t = (float)(rng.NextDouble() * (keyCount - 1));

                    // var logText = $"t = {t:F3}, custom = {custom.Evaluate(t)}, unity = {unity.Evaluate(t)}";
                    // Debug.Log($"[{nameof(AnimationCurveEvaluationTest)}] {logText}");

                    Assert.That(custom.Evaluate(t),
                                Is.EqualTo(unity.Evaluate(t)).Within(Epsilon),
                                $"Mismatch at t = {t:F3} (Trial: {trial})");
                }
            }
        }
    }
}
