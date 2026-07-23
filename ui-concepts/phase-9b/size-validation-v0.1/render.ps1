Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root 'renders'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$fontPath = Join-Path $root '../../../sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf'
$fonts = New-Object System.Drawing.Text.PrivateFontCollection
$fonts.AddFontFile((Resolve-Path $fontPath))
$family = $fonts.Families[0]

function Color([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }
function Font([float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    New-Object System.Drawing.Font($family, $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
}
function Draw-Card($card) {
    $w = if ($card.compact) { 160 } else { 240 }
    $h = if ($card.compact) { 240 } else { 360 }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear((Color '#f4ecd9'))
    $edge = Color $card.edge
    $tab = Color $card.tab
    $border = New-Object System.Drawing.Pen($edge, $(if($card.compact){2}else{3}))
    $g.DrawRectangle($border, 1, 1, $w-3, $h-3)

    $artH = if ($card.compact) { 105 } else { 166 }
    $img = [System.Drawing.Image]::FromFile((Resolve-Path (Join-Path $root $card.art)))
    $srcRatio = $w / $artH
    if($card.crop){
        $bx=$card.crop[0];$by=$card.crop[1];$bw=$card.crop[2];$bh=$card.crop[3]
        if(($bw/$bh) -gt $srcRatio){
            $srcH=$bh;$srcW=[int]($srcH*$srcRatio)
            $sx=[int]($bx+($bw-$srcW)/2);$sy=$by
        } else {
            $srcW=$bw;$srcH=[int]($srcW/$srcRatio)
            $sx=$bx;$sy=[int]($by+($bh-$srcH)/2)
        }
    } else {
        $srcW = $img.Width
        $srcH = [int]($srcW / $srcRatio)
        if ($srcH -gt $img.Height) { $srcH=$img.Height; $srcW=[int]($srcH*$srcRatio) }
        $sx=[int](($img.Width-$srcW)/2);$sy=0
    }
    $g.DrawImage($img, ([System.Drawing.Rectangle]::new(4,4,($w-8),$artH)), ([System.Drawing.Rectangle]::new($sx,$sy,$srcW,$srcH)), [System.Drawing.GraphicsUnit]::Pixel)
    $img.Dispose()

    $nameY = if ($card.compact) { 96 } else { 151 }
    $nameH = if ($card.compact) { 25 } else { 35 }
    $g.FillRectangle((New-Object System.Drawing.SolidBrush($tab)), 7, $nameY, $w-14, $nameH)
    $nameFont=Font $(if($card.compact){13}else{20}) ([System.Drawing.FontStyle]::Bold)
    $sf=New-Object System.Drawing.StringFormat
    $sf.Alignment='Center';$sf.LineAlignment='Center'
    $g.DrawString($card.name,$nameFont,[System.Drawing.Brushes]::White,([System.Drawing.RectangleF]::new(7,$nameY,($w-14),$nameH)),$sf)

    $tierSize=if($card.compact){22}else{29}
    $g.FillEllipse((New-Object System.Drawing.SolidBrush((Color '#f2dfaa'))),5,5,$tierSize,$tierSize)
    $g.DrawEllipse((New-Object System.Drawing.Pen($edge,2)),5,5,$tierSize,$tierSize)
    $tierFont=Font $(if($card.compact){11}else{16}) ([System.Drawing.FontStyle]::Bold)
    $tierBrush=New-Object System.Drawing.SolidBrush((Color '#30271d'))
    $g.DrawString([string]$card.tier,$tierFont,$tierBrush,([System.Drawing.RectangleF]::new(5,5,$tierSize,$tierSize)),$sf)

    $metaY=if($card.compact){123}else{190}
    $metaFont=Font $(if($card.compact){7}else{10})
    $g.DrawString($card.race,$metaFont,(New-Object System.Drawing.SolidBrush($edge)),7,$metaY)
    $kwSize=$g.MeasureString($card.keywords,$metaFont)
    $g.DrawString($card.keywords,$metaFont,(New-Object System.Drawing.SolidBrush($edge)),$w-$kwSize.Width-7,$metaY)

    $descY=if($card.compact){138}else{211}
    $descH=if($card.compact){66}else{101}
    $descFont=Font $(if($card.compact){7.6}else{12.5})
    $descFormat=New-Object System.Drawing.StringFormat
    $descFormat.Trimming='None'
    $displayDesc=if($card.compact -and $card.compactDesc){$card.compactDesc}else{$card.desc}
    $g.DrawString($displayDesc,$descFont,(New-Object System.Drawing.SolidBrush((Color '#29231c'))),([System.Drawing.RectangleF]::new(10,$descY,($w-20),$descH)),$descFormat)

    $stat=if($card.compact){27}else{39}
    $statY=$h-$stat-7
    $g.FillEllipse((New-Object System.Drawing.SolidBrush((Color '#9d432a'))),7,$statY,$stat,$stat)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush((Color '#4f7b50'))),$w-$stat-7,$statY,$stat,$stat)
    $statFont=Font $(if($card.compact){15}else{22}) ([System.Drawing.FontStyle]::Bold)
    $g.DrawString([string]$card.atk,$statFont,[System.Drawing.Brushes]::White,([System.Drawing.RectangleF]::new(7,$statY,$stat,$stat)),$sf)
    $g.DrawString([string]$card.hp,$statFont,[System.Drawing.Brushes]::White,([System.Drawing.RectangleF]::new(($w-$stat-7),$statY,$stat,$stat)),$sf)
    $badgeFont=Font $(if($card.compact){7}else{9})
    $badgeSize=$g.MeasureString($card.badge,$badgeFont)
    $g.DrawString($card.badge,$badgeFont,(New-Object System.Drawing.SolidBrush($edge)),($w-$badgeSize.Width)/2,$h-$badgeSize.Height-10)
    $iconX=[int](($w-$badgeSize.Width)/2)-17
    $iconY=$h-22
    $iconPen=New-Object System.Drawing.Pen($edge,2)
    if($card.stateIcon -eq 'shield'){
        $pts=[System.Drawing.Point[]]@(
            [System.Drawing.Point]::new($iconX,$iconY-9),
            [System.Drawing.Point]::new($iconX+12,$iconY-9),
            [System.Drawing.Point]::new($iconX+11,$iconY),
            [System.Drawing.Point]::new($iconX+6,$iconY+5),
            [System.Drawing.Point]::new($iconX+1,$iconY)
        )
        $g.DrawPolygon($iconPen,$pts)
    } elseif($card.stateIcon -eq 'growth'){
        $g.DrawLine($iconPen,$iconX+6,$iconY+5,$iconX+6,$iconY-7)
        $g.DrawArc($iconPen,$iconX-1,$iconY-8,8,7,200,160)
        $g.DrawArc($iconPen,$iconX+6,$iconY-11,8,8,20,160)
    }
    $iconPen.Dispose()

    if($card.golden){
        $goldPen=New-Object System.Drawing.Pen((Color '#d1a334'),4)
        $g.DrawRectangle($goldPen,3,3,$w-7,$h-7)
        $seal=if($card.compact){24}else{34}
        $sealX=$w-$seal-9;$sealY=8
        $g.FillEllipse((New-Object System.Drawing.SolidBrush((Color '#d5aa3c'))),$sealX,$sealY,$seal,$seal)
        $g.DrawEllipse((New-Object System.Drawing.Pen((Color '#fff0a8'),2)),$sealX+2,$sealY+2,$seal-4,$seal-4)
        $starFont=Font $(if($card.compact){13}else{20}) ([System.Drawing.FontStyle]::Bold)
        $g.DrawString('★',$starFont,(New-Object System.Drawing.SolidBrush((Color '#fff4bd'))),([System.Drawing.RectangleF]::new($sealX,$sealY,$seal,$seal)),$sf)
        $goldPen.Dispose()
    }
    $path=Join-Path $out ($card.id+'.png')
    $bmp.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
    $border.Dispose();$g.Dispose();$bmp.Dispose()
    return $path
}

$cards=@(
@{id='shield-full';name='铸魂盾侍';compact=$false;tier=1;atk=1;hp=3;race='铸魂';keywords='嘲讽';badge='铸魂 · 护盾';stateIcon='shield';desc='战斗开始：获得护盾。';edge='#78391f';tab='#8f4329';art='art/forge-soul-shield-squire-storybook-v0.2.png'},
@{id='shield-compact';name='铸魂盾侍';compact=$true;tier=1;atk=1;hp=3;race='铸魂';keywords='嘲讽';badge='铸魂 · 护盾';stateIcon='shield';compactDesc='嘲讽 · 开场护盾';desc='战斗开始：获得护盾。';edge='#78391f';tab='#8f4329';art='art/forge-soul-shield-squire-storybook-v0.2.png'},
@{id='hoof-full';name='万蹄奔潮';compact=$false;tier=5;atk=7;hp=8;race='荒灵';keywords='召唤强化';badge='荒灵 · 永久成长';stateIcon='growth';desc='每当你召唤随从，使其本场获得 +2 攻击；若为召唤物，再使其立即攻击一次。每场前两个召唤物死亡后，本随从永久获得 +1 攻击。';edge='#3f633f';tab='#557646';art='../style-tiles/style-tile-wandering-storybook-v0.3.png';crop=@(240,58,205,370)},
@{id='hoof-compact';name='万蹄奔潮';compact=$true;tier=5;atk=7;hp=8;race='荒灵';keywords='召唤强化';badge='荒灵 · 永久成长';stateIcon='growth';compactDesc='召唤强化 · 立即攻击 · 永久成长';desc='每当你召唤随从，使其本场获得 +2 攻击；若为召唤物，再使其立即攻击一次。每场前两个召唤物死亡后，本随从永久获得 +1 攻击。';edge='#3f633f';tab='#557646';art='../style-tiles/style-tile-wandering-storybook-v0.3.png';crop=@(240,58,205,370)},
@{id='sky-full';name='天穹契约者';compact=$false;tier=5;atk=4;hp=8;race='星契';keywords='刷新成长';badge='星契 · 永久成长';stateIcon='growth';desc='每个商店阶段中，每刷新 4 次，所有友方星契永久获得 +1/+1。';edge='#3d4e86';tab='#43558d';art='../style-tiles/style-tile-wandering-storybook-v0.3.png';crop=@(455,58,190,370)},
@{id='sky-gold-full';name='天穹契约者';compact=$false;golden=$true;tier=5;atk=8;hp=16;race='星契';keywords='金色 · 刷新成长';badge='星契 · 永久成长';stateIcon='growth';desc='每个商店阶段中，每刷新 3 次，所有友方星契永久获得 +1/+1。';edge='#3d4e86';tab='#43558d';art='../style-tiles/style-tile-wandering-storybook-v0.3.png';crop=@(455,58,190,370)},
@{id='sky-compact';name='天穹契约者';compact=$true;tier=5;atk=4;hp=8;race='星契';keywords='刷新成长';badge='星契 · 永久成长';stateIcon='growth';compactDesc='刷新成长 · 群体成长 · 永久 +1/+1';desc='每个商店阶段中，每刷新 4 次，所有友方星契永久获得 +1/+1。';edge='#3d4e86';tab='#43558d';art='../style-tiles/style-tile-wandering-storybook-v0.3.png';crop=@(455,58,190,370)},
@{id='furnace-full';name='不熄炉王';compact=$false;tier=5;atk=6;hp=8;race='铸魂';keywords='嘲讽';badge='铸魂 · 护盾链';stateIcon='shield';desc='战斗开始：获得护盾。每当一个友方铸魂失去护盾，使另一个无护盾友方铸魂获得护盾。每场战斗最多触发 2 次。';edge='#6b261c';tab='#7b2d21';art='../style-tiles/style-tile-wandering-storybook-v0.3.png';crop=@(260,440,300,300)}
)
$paths=@();foreach($card in $cards){$paths+=Draw-Card $card}

$overview=New-Object System.Drawing.Bitmap(1440,900)
$og=[System.Drawing.Graphics]::FromImage($overview)
$og.Clear((Color '#d8cfbb'))
$title=Font 24 ([System.Drawing.FontStyle]::Bold)
$small=Font 12
$og.DrawString('旅团绘本 · 尺寸真实性验证 v0.1（全部按 1:1 像素显示）',$title,(New-Object System.Drawing.SolidBrush((Color '#28231d'))),20,14)
$og.DrawString('Compact 保留完整描述，用于暴露真实拥挤问题。',$small,(New-Object System.Drawing.SolidBrush((Color '#554b3e'))),22,49)
$positions=@(@(20,80),@(268,80),@(448,80),@(696,80),@(876,80),@(1124,80),@(20,470),@(268,470))
for($i=0;$i -lt $paths.Count;$i++){
    $img=[System.Drawing.Image]::FromFile($paths[$i])
    $og.DrawImageUnscaled($img,$positions[$i][0],$positions[$i][1])
    $img.Dispose()
}
$overview.Save((Join-Path $out 'size-validation-overview-1x.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$og.Dispose();$overview.Dispose();$fonts.Dispose()
