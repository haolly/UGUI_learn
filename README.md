通过抄一遍代码来学习UGUI 的工作原理

可以被剪裁的对象：IClippable
IClippable 的cull 接口是对 clip 的一个优化？ 如果没有在rect里面，直接cull掉
可以被mask 的对象: IMaskable
这两个总是一起的？

RectMask2D 用来mask 那些 IClippable
mask 的时候会去遍历自己的所有父节点RectMask2D 来做交集， ref
RectMask2D:PerformClipping()
单个RectMask2D 的大小是根据Canvas 的大小决定的

RectMask2D VS Mask
RectMask2D 是用 rectClip + cull 来实现
Mask 是用 todo


RectMask2D:OnEnable/OnDisable --> Notify2DMaskStageChanged()
导致所有孩子节点RecalculateClipping(),然后孩子节点将自己添加到父节点的clippable 列表里面去，AddClippable()

MaskableGraphic OnEnable/OnDisable/OnTransformParentChanged/.. -> 导致
UpdateClipParent() --> AddClippable(), 最后 ClipperRegistry:Cull() -->PerformClipping()

ICanvasElement 表示一个界面，CanvasUpdate 有5种枚举值，
PreLayout/Layout/PostLayout/PreRender/LateRender, 按照先后顺序执行

CanvasUpdateRegistry:PerfromUpdate()  所有UI Update 总入口，有两个列表， layourRebuildQueue
(mesh的重建） + PerformingGraphicUpdateQueue, layout rebuild 之后就会执行
 cull操作(ClipperRegistry:Cull())
Graphic 和 LayoutRebuilder 都实现了 ICanvasElement
接口，前者处理CanvasUpdate.PreRender，后者处理 CanvasUpdate.Layout
渲染的时候使用材质 materialForRendering, 这个是经过所有的 IMaterialModifier
处理过之后的材质；

IMaterialModifier 有哪些？TODO


所有显示对象的基类是 Graphic有layoutDirty/verticesDirty/materialDirty
三个层次的dirty

TODO
啥时候被加入layout 列表？
OnRecttransformDimensionsChange/OnTransformParentChanged/
1，Graphic:SetLayoutDirty()
LayoutRebuilder 负责具体执行

啥时候被加入graphic 列表？
todo

