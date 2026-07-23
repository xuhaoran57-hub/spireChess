Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$out=Join-Path $root 'renders';New-Item -ItemType Directory -Force -Path $out|Out-Null
$fontPath=Join-Path $root '../../../sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf'
$fonts=New-Object System.Drawing.Text.PrivateFontCollection
$fonts.AddFontFile((Resolve-Path $fontPath));$family=$fonts.Families[0]
function C($hex){[System.Drawing.ColorTranslator]::FromHtml($hex)}
function F($size,$style=[System.Drawing.FontStyle]::Regular){New-Object System.Drawing.Font($family,$size,$style,[System.Drawing.GraphicsUnit]::Pixel)}
function Badge($g,$x,$y,$kind){
 $colors=@{taunt='#a94e32';death='#6c477d';cleave='#287b73'};$fill=New-Object System.Drawing.SolidBrush((C $colors[$kind]))
 $g.FillEllipse($fill,$x,$y,36,36);$g.DrawEllipse((New-Object System.Drawing.Pen((C '#f4dfb1'),2)),$x+2,$y+2,32,32)
 $pen=New-Object System.Drawing.Pen([System.Drawing.Color]::White,3)
 if($kind-eq'taunt'){$g.DrawRectangle($pen,$x+11,$y+9,14,15);$g.DrawLine($pen,$x+5,$y+18,$x+11,$y+18);$g.DrawLine($pen,$x+31,$y+18,$x+25,$y+18)}
 elseif($kind-eq'death'){$g.DrawArc($pen,$x+10,$y+8,16,17,185,170);$g.DrawLine($pen,$x+9,$y+22,$x+27,$y+22);$g.FillEllipse([System.Drawing.Brushes]::White,$x+16,$y+25,4,4)}
 else{$g.DrawLine($pen,$x+18,$y+7,$x+18,$y+28);$g.DrawLine($pen,$x+18,$y+16,$x+7,$y+25);$g.DrawLine($pen,$x+18,$y+16,$x+29,$y+25)}
 $pen.Dispose();$fill.Dispose()
}
function Shield($g,$x,$y,$w,$h,$broken=$false){
 $pen=New-Object System.Drawing.Pen((C '#74cbe6'),5);$pen.DashStyle=if($broken){'Dash'}else{'Solid'}
 $g.DrawRectangle($pen,$x-4,$y-4,$w+8,$h+8)
 if($broken){$g.DrawLine($pen,$x+$w-18,$y-4,$x+$w-32,$y+25);$g.DrawLine($pen,$x+$w-32,$y+25,$x+$w-15,$y+42)}
 $pen.Dispose()
}
function Card($g,$imgPath,$x,$y,$label,$badges=@(),$shield=$false,$broken=$false,$target=$false,$attacker=$false){
 $img=[System.Drawing.Image]::FromFile((Resolve-Path $imgPath));$g.DrawImageUnscaled($img,$x,$y);$img.Dispose()
 if($shield){Shield $g $x $y 160 240 $broken}
 $n=0;foreach($b in $badges){Badge $g ($x+132) ($y+12+$n*42) $b;$n++}
 if($target){$p=New-Object System.Drawing.Pen((C '#d94d3d'),5);$g.DrawRectangle($p,$x-8,$y-8,176,256);$p.Dispose()}
 if($attacker){$p=New-Object System.Drawing.Pen((C '#e49a36'),6);$g.DrawArc($p,$x-10,$y-10,180,260,200,140);$p.Dispose()}
 $font=F 18 ([System.Drawing.FontStyle]::Bold);$sf=New-Object System.Drawing.StringFormat;$sf.Alignment='Center'
 $g.DrawString($label,$font,(New-Object System.Drawing.SolidBrush((C '#fff5d6'))),([System.Drawing.RectangleF]::new($x-10,$y+246,180,28)),$sf)
}
function Panel($g,$x,$y,$w,$h,$title,$caption){
 $g.FillRectangle((New-Object System.Drawing.SolidBrush((C '#eee4cb'))),$x,$y,$w,$h)
 $g.DrawRectangle((New-Object System.Drawing.Pen((C '#7e6a4c'),2)),$x,$y,$w,$h)
 $g.DrawString($title,(F 21 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#33291f'))),$x+14,$y+10)
 $g.DrawString($caption,(F 14),(New-Object System.Drawing.SolidBrush((C '#5d5040'))),([System.Drawing.RectangleF]::new($x+14,$y+$h-52,$w-28,42)))
}

$cardRoot=Join-Path $root '../size-validation-v0.1/renders'
$shield=Join-Path $cardRoot 'shield-compact.png';$hoof=Join-Path $cardRoot 'hoof-compact.png';$sky=Join-Path $cardRoot 'sky-compact.png'
$battle=New-Object System.Drawing.Bitmap(1920,1080);$g=[System.Drawing.Graphics]::FromImage($battle);$g.SmoothingMode='AntiAlias'
$g.Clear((C '#1d2a28'));$g.FillRectangle((New-Object System.Drawing.SolidBrush((C '#30443d'))),0,0,1920,1080)
$g.DrawString('旅团绘本 · 战斗关键词与状态表现验证 v0.1',(F 34 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#f5e7c8'))),48,28)
$g.DrawString('商店/手牌不显示徽章；以下仅为上阵区与正式战斗界面。',(F 18),(New-Object System.Drawing.SolidBrush((C '#d4c39e'))),50,76)
$g.DrawString('敌方战斗区',(F 20 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#efc3a3'))),70,133)
Card $g $sky 120 170 '无关键词对照'
Card $g $hoof 370 170 '亡语' @('death')
Card $g $hoof 620 170 '溅射' @('cleave')
Card $g $shield 870 170 '嘲讽' @('taunt')
Card $g $shield 1120 170 '嘲讽 + 护盾' @('taunt') $true
Card $g $sky 1370 170 '当前目标' @() $false $false $true
$g.DrawString('玩家战斗区',(F 20 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#b8e2cd'))),70,520)
Card $g $shield 120 560 '当前护盾' @('taunt') $true
Card $g $shield 370 560 '护盾破裂瞬间' @('taunt') $true $true
Card $g $shield 620 560 '护盾已消失' @('taunt')
Card $g $hoof 870 560 '攻击者' @('cleave') $false $false $false $true
Card $g $sky 1120 560 '临时 +2/+1'
Card $g $hoof 1370 560 '亡语待触发' @('death')
$g.DrawString('徽章：稳定规则关键词　　整卡覆盖：当前状态　　事件动画：获得/抵挡/失去/触发',(F 18),(New-Object System.Drawing.SolidBrush((C '#f5e7c8'))),120,920)
$g.DrawString('战吼为一次性入场演出，不常驻；永久成长不设专属图标。',(F 18),(New-Object System.Drawing.SolidBrush((C '#f5e7c8'))),120,955)
$battle.Save((Join-Path $out 'battle-keyword-state-validation-1920x1080.png'),[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$battle.Dispose()

$story=New-Object System.Drawing.Bitmap(1920,1080);$g=[System.Drawing.Graphics]::FromImage($story);$g.SmoothingMode='AntiAlias';$g.Clear((C '#d8cfbb'))
$g.DrawString('旅团绘本 · 战斗事件分镜 v0.1',(F 32 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#30271e'))),42,22)
$rows=@(
 @('护盾',@('获得：水彩边缘绘入','抵挡：蓝白飞溅，生命不变','失去：边缘开裂并消散','触发：再播放失盾联动')),
 @('亡语',@('死亡：卡牌褪色','徽章：紫色铃铛点亮','结算：保持卡牌到效果结束','完成：盖印后离场')),
 @('溅射',@('选定：主目标红框','预告：支线指向相邻目标','命中：主目标先反馈','扩散：相邻目标同步斩痕'))
)
for($r=0;$r -lt 3;$r++){
 $y=82+$r*325;$g.DrawString($rows[$r][0],(F 24 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#3d3327'))),30,$y+120)
 for($c=0;$c -lt 4;$c++){
  $x=150+$c*430;Panel $g $x $y 390 285 ($c+1).ToString() $rows[$r][1][$c]
  if($r-eq0){Card $g $shield ($x+115) ($y+38) '' @('taunt') ($c-lt3) ($c-eq2)}
  elseif($r-eq1){Card $g $hoof ($x+115) ($y+38) '' @('death');if($c-ge1){$g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(80,70,55,70))),$x+115,$y+38,160,240)}}
  else{Card $g $hoof ($x+115) ($y+38) '' @('cleave') $false $false ($c-eq0);if($c-ge1){$p=New-Object System.Drawing.Pen((C '#318b81'),4);$g.DrawLine($p,$x+195,$y+160,$x+80,$y+210);$g.DrawLine($p,$x+195,$y+160,$x+310,$y+210);$p.Dispose()}}
 }
}
$story.Save((Join-Path $out 'battle-event-storyboards-1920x1080.png'),[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$story.Dispose();$fonts.Dispose()
