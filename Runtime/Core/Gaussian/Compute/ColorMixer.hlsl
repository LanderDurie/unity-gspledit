#ifndef COLOR_MIXER_HLSL
#define COLOR_MIXER_HLSL

// Alpha blending (standard)
half4 alphaMix(half4 fg, half4 bg) {
    // Check if foreground is black with 0 alpha (individual components)
    bool isBlack = fg.r == 0 && fg.g == 0 && fg.b == 0 && fg.a == 0;
    
    if (isBlack) {
        return bg;
    }
    
    // Standard alpha blending
    half3 blendedColor = (fg.rgb * fg.a) + (bg.rgb * (1 - fg.a));
    half blendedAlpha = fg.a + bg.a * (1 - fg.a);
    
    return half4(blendedColor, blendedAlpha);
}

// Additive blending
half4 additiveMix(half4 fg, half4 bg) {
    // Check if foreground is black (individual components)
    bool isBlack = fg.r == 0 && fg.g == 0 && fg.b == 0;
    
    if (isBlack) {
        return bg;
    }
    
    // Additive blend (good for glows, light effects)
    half3 blendedColor = min(bg.rgb + fg.rgb * fg.a, 1.0);
    half blendedAlpha = max(fg.a, bg.a);
    
    return half4(blendedColor, blendedAlpha);
}

// Multiply blending
half4 multiplyMix(half4 fg, half4 bg) {
    // Check if foreground is white or has zero alpha
    bool isWhiteOrTransparent = (fg.r == 1 && fg.g == 1 && fg.b == 1) || fg.a == 0;
    
    if (isWhiteOrTransparent) {
        return bg;
    }
    
    // Multiply blend (darkens image)
    half3 blendedColor = bg.rgb * lerp(half3(1, 1, 1), fg.rgb, fg.a);
    half blendedAlpha = bg.a;
    
    return half4(blendedColor, blendedAlpha);
}

// Overlay blending
half4 overlayMix(half4 fg, half4 bg) {
    // Check if foreground is black with 0 alpha
    bool isBlackTransparent = fg.r == 0 && fg.g == 0 && fg.b == 0 && fg.a == 0;
    
    if (isBlackTransparent) {
        return bg;
    }
    
    // Overlay blend
    half3 blendedColor;
    
    // Process each channel individually
    half r = bg.r < 0.5 ? 
        2.0 * bg.r * fg.r : 
        1.0 - 2.0 * (1.0 - bg.r) * (1.0 - fg.r);
        
    half g = bg.g < 0.5 ? 
        2.0 * bg.g * fg.g : 
        1.0 - 2.0 * (1.0 - bg.g) * (1.0 - fg.g);
        
    half b = bg.b < 0.5 ? 
        2.0 * bg.b * fg.b : 
        1.0 - 2.0 * (1.0 - bg.b) * (1.0 - fg.b);
    
    blendedColor = half3(r, g, b);
    
    // Blend based on foreground alpha
    blendedColor = lerp(bg.rgb, blendedColor, fg.a);
    half blendedAlpha = bg.a;
    
    return half4(blendedColor, blendedAlpha);
}

// Soft Light blending - gentler version of overlay
half4 softLightMix(half4 fg, half4 bg) {
    // Check if foreground is black with no alpha
    bool isBlackTransparent = fg.r == 0 && fg.g == 0 && fg.b == 0 && fg.a == 0;
    
    if (isBlackTransparent) {
        return bg;
    }
    
    // Soft light blend algorithm
    half3 adjustedFg = lerp(half3(0.5, 0.5, 0.5), fg.rgb, fg.a);
    
    // Process each channel individually
    half r, g, b;
    
    // Red channel
    if (adjustedFg.r <= 0.5) {
        r = bg.r - (1 - 2 * adjustedFg.r) * bg.r * (1 - bg.r);
    } else {
        half d;
        if (bg.r <= 0.25) {
            d = ((16 * bg.r - 12) * bg.r + 4) * bg.r;
        } else {
            d = sqrt(bg.r);
        }
        r = bg.r + (2 * adjustedFg.r - 1) * (d - bg.r);
    }
    
    // Green channel
    if (adjustedFg.g <= 0.5) {
        g = bg.g - (1 - 2 * adjustedFg.g) * bg.g * (1 - bg.g);
    } else {
        half d;
        if (bg.g <= 0.25) {
            d = ((16 * bg.g - 12) * bg.g + 4) * bg.g;
        } else {
            d = sqrt(bg.g);
        }
        g = bg.g + (2 * adjustedFg.g - 1) * (d - bg.g);
    }
    
    // Blue channel
    if (adjustedFg.b <= 0.5) {
        b = bg.b - (1 - 2 * adjustedFg.b) * bg.b * (1 - bg.b);
    } else {
        half d;
        if (bg.b <= 0.25) {
            d = ((16 * bg.b - 12) * bg.b + 4) * bg.b;
        } else {
            d = sqrt(bg.b);
        }
        b = bg.b + (2 * adjustedFg.b - 1) * (d - bg.b);
    }
    
    half3 blendedColor = half3(r, g, b);
    return half4(blendedColor, bg.a);
}

// A simpler alternative that doesn't change the background when fg is black
half4 simpleMix(half4 fg, half4 bg) {
    // Skip processing if foreground color has zero intensity or alpha
    float intensity = fg.r + fg.g + fg.b;
    if (intensity < 0.001 || fg.a < 0.001) {
        return bg;
    }
    
    // Regular alpha blending
    return half4(
        lerp(bg.rgb, fg.rgb, fg.a),
        max(fg.a, bg.a)
    );
}

half4 fullFg(half4 fg, half4 bg) {
    return fg;
}

half4 fullBg(half4 fg, half4 bg) {
    return bg;
}

#endif