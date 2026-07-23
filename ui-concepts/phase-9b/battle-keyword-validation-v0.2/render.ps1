Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$out=Join-Path $root 'renders';New-Item -ItemType Directory -Force -Path $out|Out-Null
$fontPath=Join-Path $root '../../../sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf'
$fonts=New-Object System.Drawing.Text.PrivateFontCollection;$fonts.AddFontFile((Resolve-Path $fontPath));$family=$fonts.Families[0]
function C($hex){[System.Drawing.ColorTranslator]::FromHtml($hex)}
function F($n,$s=[System.Drawing.FontStyle]::Regular){New-Object System.Drawing.Font($family,$n,$s,[System.Drawing.GraphicsUnit]::Pixel)}
function Poly($g,$brush,$pen,$pts){$p=[System.Drawing.Point[]]$pts;$g.FillPolygon($brush,$p);$g.DrawPolygon($pen,$p)}
function Bookmark($g,$x,$y,$kind,$index=0,$count=''){
 $top=$y+$index*42;$shadow=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(95,15,12,9))
 $colors=@{taunt='#a94d2f';death='#684276';cleave='#277a70';overflow='#b78a45'}
 $fill=New-Object System.Drawing.SolidBrush((C $colors[$kind]));$edge=New-Object System.Drawing.Pen((C '#f1d9a5'),2)
 $pts=@([System.Drawing.Point]::new($x+4,$top+4),[System.Drawing.Point]::new($x+36,$top+4),[System.Drawing.Point]::new($x+36,$top+33),[System.Drawing.Point]::new($x+20,$top+43),[System.Drawing.Point]::new($x+4,$top+33))
 $shadowPts=$pts|%{[System.Drawing.Point]::new($_.X+4,$_.Y+4)};Poly $g $shadow (New-Object System.Drawing.Pen([System.Drawing.Color]::Transparent,1)) $shadowPts;Poly $g $fill $edge $pts
 $texture=New-Object System.Drawing.Drawing2D.HatchBrush([System.Drawing.Drawing2D.HatchStyle]::ForwardDiagonal,[System.Drawing.Color]::FromArgb(34,255,255,255),[System.Drawing.Color]::Transparent)
 $g.FillPolygon($texture,[System.Drawing.Point[]]$pts)
 $stitch=New-Object System.Drawing.Pen((C '#f7e7bd'),1);$stitch.DashPattern=@(2,2);$g.DrawLine($stitch,$x+8,$top+9,$x+32,$top+9);$g.DrawLine($stitch,$x+8,$top+29,$x+20,$top+38);$g.DrawLine($stitch,$x+32,$top+29,$x+20,$top+38)
 $ink=New-Object System.Drawing.Pen([System.Drawing.Color]::White,2.4)
 if($kind-eq'taunt'){$g.DrawRectangle($ink,$x+13,$top+13,14,13);$g.DrawLine($ink,$x+8,$top+20,$x+13,$top+20);$g.DrawLine($ink,$x+32,$top+20,$x+27,$top+20)}
 elseif($kind-eq'death'){$wax=New-Object System.Drawing.SolidBrush((C '#8d5a96'));$g.FillEllipse($wax,$x+11,$top+11,18,18);$g.DrawArc($ink,$x+14,$top+12,12,12,190,160);$g.DrawLine($ink,$x+13,$top+24,$x+27,$top+24);$g.DrawLine($ink,$x+22,$top+13,$x+18,$top+27)}
 elseif($kind-eq'cleave'){$g.DrawLine($ink,$x+20,$top+11,$x+20,$top+30);$g.DrawLine($ink,$x+20,$top+18,$x+10,$top+28);$g.DrawLine($ink,$x+20,$top+18,$x+30,$top+28)}
 else{$sf=New-Object System.Drawing.StringFormat;$sf.Alignment='Center';$sf.LineAlignment='Center';$g.DrawString($count,(F 14 ([System.Drawing.FontStyle]::Bold)),[System.Drawing.Brushes]::White,([System.Drawing.RectangleF]::new($x+5,$top+6,30,30)),$sf)}
 $ink.Dispose();$stitch.Dispose();$texture.Dispose();$edge.Dispose();$fill.Dispose();$shadow.Dispose()
}
function ShieldOverlay($g,$x,$y,$state){
 $cyan=C '#73cfe5';$p1=New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(205,$cyan),5);$p2=New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90,$cyan),11)
 $pts=@([System.Drawing.Point]::new($x-5,$y+14),[System.Drawing.Point]::new($x+15,$y-7),[System.Drawing.Point]::new($x+145,$y-7),[System.Drawing.Point]::new($x+165,$y+14),[System.Drawing.Point]::new($x+165,$y+205),[System.Drawing.Point]::new($x+145,$y+247),[System.Drawing.Point]::new($x+15,$y+247),[System.Drawing.Point]::new($x-5,$y+205))
 if($state-eq'full'){$g.DrawPolygon($p2,[System.Drawing.Point[]]$pts);$g.DrawPolygon($p1,[System.Drawing.Point[]]$pts)}
 elseif($state-eq'cracked'){
  $p1.DashPattern=@(10,4);$g.DrawPolygon($p1,[System.Drawing.Point[]]$pts)
  $cr=New-Object System.Drawing.Pen((C '#d9f7ff'),3);$g.DrawLine($cr,$x+151,$y+20,$x+124,$y+62);$g.DrawLine($cr,$x+124,$y+62,$x+143,$y+91);$g.DrawLine($cr,$x+124,$y+62,$x+93,$y+78);$g.DrawLine($cr,$x+143,$y+91,$x+128,$y+119);$cr.Dispose()
 } elseif($state-eq'gone'){
  $frag=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(150,$cyan))
  $g.FillPolygon($frag,[System.Drawing.Point[]]@([System.Drawing.Point]::new($x+160,$y+25),[System.Drawing.Point]::new($x+180,$y+15),[System.Drawing.Point]::new($x+169,$y+45)))
  $g.FillPolygon($frag,[System.Drawing.Point[]]@([System.Drawing.Point]::new($x-5,$y+190),[System.Drawing.Point]::new($x-21,$y+205),[System.Drawing.Point]::new($x+2,$y+218)));$frag.Dispose()
 }
 $p1.Dispose();$p2.Dispose()
}
function PutCard($g,$path,$x,$y,$tabs=@(),$shield='',$caption=''){
 $img=[System.Drawing.Image]::FromFile((Resolve-Path $path));$g.DrawImageUnscaled($img,$x,$y);$img.Dispose()
 if($shield){ShieldOverlay $g $x $y $shield}
 for($i=0;$i-lt$tabs.Count;$i++){if($tabs[$i]-like'+*'){Bookmark $g ($x+150) ($y+8) overflow $i $tabs[$i]}else{Bookmark $g ($x+150) ($y+8) $tabs[$i] $i}}
 if($caption){$sf=New-Object System.Drawing.StringFormat;$sf.Alignment='Center';$g.DrawString($caption,(F 18 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#fff0c9'))),([System.Drawing.RectangleF]::new($x-12,$y+252,210,28)),$sf)}
}
function Caption($g,$x,$y,$text){$g.DrawString($text,(F 16),(New-Object System.Drawing.SolidBrush((C '#4d4032'))),([System.Drawing.RectangleF]::new($x,$y,340,42)))}

$card=Join-Path $root '../size-validation-v0.1/renders/shield-compact.png'
$hoof=Join-Path $root '../size-validation-v0.1/renders/hoof-compact.png'
$bgPath=Join-Path $root '../../unity-validation/pf-battle-screen-v0.1/battle-screen-1920x1080.png'
$bg=[System.Drawing.Image]::FromFile((Resolve-Path $bgPath))
$scene=New-Object System.Drawing.Bitmap(1920,1080);$g=[System.Drawing.Graphics]::FromImage($scene);$g.SmoothingMode='AntiAlias';$g.DrawImage($bg,0,0,1920,1080)
$veil=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(105,14,25,24));$g.FillRectangle($veil,0,0,1920,1080)
$g.DrawString('旅团绘本 · 战斗关键词书签与护盾覆盖 v0.2',(F 30 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#fff0cc'))),42,26)
$g.DrawString('正式战斗界面 · 160×240 实际卡牌 · 右侧书签栏',(F 17),(New-Object System.Drawing.SolidBrush((C '#e2cca0'))),45,67)
$xs=@(205,505,805,1105,1405);$names=@('单关键词','双关键词','三关键词','三关键词 + 护盾','四关键词溢出')
$sets=@(@('taunt'),@('taunt','death'),@('taunt','death','cleave'),@('taunt','death','cleave'),@('taunt','death','+2'))
for($i=0;$i-lt5;$i++){PutCard $g $card $xs[$i] 650 $sets[$i] $(if($i-eq3){'full'}else{''}) $names[$i]}
$g.DrawString('嘲讽：陶土粗布盾旗　　亡语：紫灰破蜡封　　溅射：青绿分叉皮签　　+N：黄褐目录页签',(F 17),(New-Object System.Drawing.SolidBrush((C '#fff0cc'))),220,980)
$scene.Save((Join-Path $out 'battle-bookmark-shield-validation-1920x1080.png'),[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$scene.Dispose();$bg.Dispose()

$story=New-Object System.Drawing.Bitmap(1920,1080);$g=[System.Drawing.Graphics]::FromImage($story);$g.SmoothingMode='AntiAlias';$g.Clear((C '#d9cfb9'))
$g.DrawString('旅团绘本 · 逐帧关键姿势 v0.2',(F 30 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#30271d'))),35,22)
$titles=@('护盾水彩绘入','抵挡飞溅','裂纹扩散','碎片消散','亡语蜡封开裂','溅射书签展开与目标连线')
for($i=0;$i-lt6;$i++){
 $col=$i%3;$row=[int][Math]::Floor($i/3);$x=35+$col*625;$y=75+$row*490
 $g.FillRectangle((New-Object System.Drawing.SolidBrush((C '#efe4c9'))),$x,$y,590,450);$g.DrawRectangle((New-Object System.Drawing.Pen((C '#826e4e'),2)),$x,$y,590,450)
 $g.DrawString(($i+1).ToString()+'  '+$titles[$i],(F 21 ([System.Drawing.FontStyle]::Bold)),(New-Object System.Drawing.SolidBrush((C '#3d3125'))),$x+16,$y+12)
 if($i-lt4){
  PutCard $g $card ($x+205) ($y+78) @('taunt') $(if($i-eq0){'full'}elseif($i-eq1){'full'}elseif($i-eq2){'cracked'}else{'gone'})
  if($i-eq1){$splash=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(170,(C '#a8efff')));for($n=0;$n-lt9;$n++){$g.FillEllipse($splash,$x+360+$n*9,$y+150+($n%3)*13,8+$n,5+$n)};$splash.Dispose()}
 } elseif($i-eq4){
  PutCard $g $hoof ($x+205) ($y+78) @('death')
  $cr=New-Object System.Drawing.Pen((C '#f1c1f6'),3);$g.DrawLine($cr,$x+373,$y+98,$x+354,$y+140);$g.DrawLine($cr,$x+354,$y+140,$x+380,$y+165);$cr.Dispose()
 } else {
  PutCard $g $hoof ($x+205) ($y+78) @('cleave')
  $line=New-Object System.Drawing.Pen((C '#2d887e'),5);$line.EndCap='ArrowAnchor';$g.DrawLine($line,$x+285,$y+210,$x+105,$y+330);$g.DrawLine($line,$x+285,$y+210,$x+465,$y+330);$line.Dispose()
 }
}
$story.Save((Join-Path $out 'battle-animation-keyposes-1920x1080.png'),[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$story.Dispose();$fonts.Dispose()
