#ifndef COLOR_MIXER_HLSL
#define COLOR_MIXER_HLSL


////////////////////////////////
/////// COLOR CONVESION ////////
////////////////////////////////

half3 RGBtoYUV(half3 rgb) {
    const half3x3 YUV_MATRIX = half3x3(
        0.2126,  0.7152,  0.0722,
       -0.09991, -0.33609, 0.436,
        0.615,   -0.55861, -0.05639
    );

    return mul(YUV_MATRIX, rgb);
}



////////////////////////////////
/////// COLOR SIMILARITY ///////
////////////////////////////////

half YUVSimilarity(half3 rgb1, half3 rgb2) {
    half3 yuv1 = RGBtoYUV(rgb1);
    half3 yuv2 = RGBtoYUV(rgb2);

    // Luminance difference
    half dY = yuv2.x - yuv1.x;
    
    // Chrominance differences
    half dU = yuv2.y - yuv1.y;
    half dV = yuv2.z - yuv1.z;

    half deltaE = sqrt( (dY * dY * 0.8) + 
                        (dU * dU * 0.1) + 
                        (dV * dV * 0.1) );

    return deltaE;
}

half CIEDE2000Similarity(half3 lab1, half3 lab2) {
    // Lightness difference
    half dL = lab2.x - lab1.x;

    // C* (chroma)
    half C1 = sqrt(lab1.y * lab1.y + lab1.z * lab1.z);
    half C2 = sqrt(lab2.y * lab2.y + lab2.z * lab2.z);
    
    half meanC = (C1 + C2) * 0.5;
    half G = 0.5 * (1.0 - sqrt((meanC * meanC) / (meanC * meanC + 25.0)));

    // Adjust a* values
    half a1Prime = lab1.y * (1.0 + G);
    half a2Prime = lab2.y * (1.0 + G);

    // Chroma using adjusted a*
    half C1Prime = sqrt(a1Prime * a1Prime + lab1.z * lab1.z);
    half C2Prime = sqrt(a2Prime * a2Prime + lab2.z * lab2.z);

    // Chroma difference
    half dCPrime = C2Prime - C1Prime;

    // Hue angles
    half h1 = atan2(lab1.z, a1Prime);
    if (h1 < 0.0) h1 += 6.283185; // Ensure non-negative hue

    half h2 = atan2(lab2.z, a2Prime);
    if (h2 < 0.0) h2 += 6.283185;

    // Hue difference
    half dHPrime = h2 - h1;
    if (abs(dHPrime) > 3.14159) {
        dHPrime -= sign(dHPrime) * 6.283185;
    }
    dHPrime = 2.0 * sqrt(C1Prime * C2Prime) * sin(dHPrime * 0.5);

    half meanH = (abs(h1 - h2) > 3.14159) ? (h1 + h2 + 6.283185) * 0.5 : (h1 + h2) * 0.5;

    // Weighting functions for lightness, chroma, and hue
    half T = 1.0 - 0.17 * cos(meanH - 0.523599) +
                  0.24 * cos(2.0 * meanH) +
                  0.32 * cos(3.0 * meanH + 0.10472) -
                  0.20 * cos(4.0 * meanH - 1.09956);

    half SL = 1.0 + ((0.015 * (lab1.x + lab2.x - 100.0) * (lab1.x + lab2.x - 100.0)) / sqrt(20.0 + (lab1.x + lab2.x - 100.0) * (lab1.x + lab2.x - 100.0)));
    half SC = 1.0 + 0.045 * meanC;
    half SH = 1.0 + 0.015 * meanC * T;

    // Rotation term (hue weighting factor)
    half deltaTheta = 0.523599 * exp(-((meanH - 4.799655) * (meanH - 4.799655)) / 0.16);
    half RT = -2.0 * sqrt(pow(meanC, 7.0) / (pow(meanC, 7.0) + pow(25.0, 7.0))) * sin(2.0 * deltaTheta);

    // CIEDE2000 distance formula
    half deltaE = sqrt((dL / SL) * (dL / SL) +
                       (dCPrime / SC) * (dCPrime / SC) +
                       (dHPrime / SH) * (dHPrime / SH) +
                       RT * (dCPrime / SC) * (dHPrime / SH));

    return deltaE;
}




////////////////////////////////
/////// COLOR MIXERS ///////////
////////////////////////////////

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

half4 shadowMix(half4 base, half4 mod) {
    half3 shadowColor = half3(0.0, 0.0, 0.0);
    float gamma = 2.2; // Standard gamma correction factor

    // Compute shadow value
    half3 shadowVal = (1 - mod.rgb) * 0.85 * mod.a;

    // Apply gamma correction to shadowVal
    shadowVal = pow(max(shadowVal, 0.0001), gamma);

    // Compute luminance
    float luminance = dot(shadowVal, half3(0.2126, 0.7152, 0.0722));

    // If luminance is too low, set shadowVal to zero
    if (luminance < 0.1) {
        shadowVal = half3(0, 0, 0);
    }

    // Mix shadow with base color
    half3 mix = shadowColor * shadowVal + (1 - shadowVal) * base.rgb;
    // return half4(mix, base.a);
    return mod;
}

half3 ExtractLightingShadowWithTransparency(half4 color) {
    // Extract the RGB lighting/shadow contribution
    half3 lightingFactor = color.rgb;
    
    // Handle the transparency by setting alpha to zero in the shadowed areas
    // If the alpha is very low, it means the area is transparent (shadowed area)
    half alphaThreshold = 0.1;  // Adjust based on desired transparency threshold
    
    // Set alpha to 0 if it is under the threshold (transparent shadowed area)
    if (color.a < alphaThreshold) {
        lightingFactor = half3(0, 0, 0); // Completely transparent
    }

    return lightingFactor;
}



half4 CustomColorMix(half4 base, half4 mod) {

    // half3 c = ExtractLightingShadowWithTransparency(mod);

    // half influence = CIEDE2000Similarity(half3(1,1,1), mod);

    // // half4 mix = mod * influence + (1 - influence) * base;

    // // mix.a = base.a;
    // // return mix;

    // // // Choose a blend factor based on the amount of light
    // // half blendFactor = saturate(lightIntensity);  // Light intensity determines how much to blend.

    // // // If lightIntensity is high (pixel is lit), prefer meshA (with lighting and shadows)
    // // // If lightIntensity is low (pixel is in shadow), prefer meshB (with texture)
    // // return lerp(meshBColor, meshAColor, blendFactor);
    // // return half4(c, 1);
    // return half4(influence, influence, influence, 1);
    
    return base * mod;
}


#endif