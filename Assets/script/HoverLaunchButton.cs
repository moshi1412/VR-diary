using UnityEngine;

public class HoverLaunchButton : MonoBehaviour
{
    // 调用这个方法，所有球一起起飞
    public void LaunchAllBalls()
    {
        foreach (var hover in MagLevHover.Instances)
        {
            if (hover == null) continue;
            hover.TriggerLaunchSequence();
        }
    }
}
