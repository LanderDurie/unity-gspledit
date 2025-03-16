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

half4 shadowMix(half4 base, half4 mod) {
    half3 shadowColor = half3(0.0, 0.0, 0.0);
    float gamma = 2.2; // Standard gamma correction factor

    // Compute shadow value
    half3 shadowVal = (1 - mod.rgb) * 0.85;

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
    return half4(mix, base.a);
}



// Constants for RGB to XYZ conversion (D65 illuminant)
#define Xn 0.95047f
#define Yn 1.00000f
#define Zn 1.08883f

// Helper function to clamp values between 0 and 1
half clamp(half value, half minVal, half maxVal) {
    return max(min(value, maxVal), minVal);
}

// RGB to XYZ conversion (D65)
half3 RGBtoXYZ(half3 rgb) {
    // Linearize RGB
    rgb = clamp(rgb, 0.0, 1.0);
    rgb = (rgb <= 0.04045) ? rgb / 12.92 : pow((rgb + 0.055) / 1.055, 2.4);

    // Apply RGB to XYZ transformation matrix (D65)
    half3 xyz;
    xyz.x = 0.4124564f * rgb.x + 0.3575761f * rgb.y + 0.1804375f * rgb.z;
    xyz.y = 0.2126729f * rgb.x + 0.7151522f * rgb.y + 0.0721750f * rgb.z;
    xyz.z = 0.0193339f * rgb.x + 0.1191920f * rgb.y + 0.9503041f * rgb.z;

    return xyz;
}

// XYZ to Lab conversion
half3 XYZtoLab(half3 xyz) {
    // Normalize to D65 reference white
    xyz.x /= Xn;
    xyz.y /= Yn;
    xyz.z /= Zn;

    // Apply the transformation for each channel
    half3 lab;
    lab.x = 116.0 * max(0.0, pow(xyz.y, 1.0 / 3.0) - 0.137931);
    lab.y = 500.0 * (pow(xyz.x, 1.0 / 3.0) - pow(xyz.y, 1.0 / 3.0));
    lab.z = 200.0 * (pow(xyz.y, 1.0 / 3.0) - pow(xyz.z, 1.0 / 3.0));

    return lab;
}

// RGB to Lab conversion
half3 RGBtoLab(half3 rgb) {
    return XYZtoLab(RGBtoXYZ(rgb));
}

// Lab to XYZ conversion
half3 LabtoXYZ(half3 lab) {
    half3 xyz;
    xyz.y = pow((lab.x + 16.0) / 116.0, 3.0);
    xyz.x = lab.y / 500.0 + xyz.y;
    xyz.z = xyz.y - lab.z / 200.0;

    // Denormalize using reference white values
    xyz.x *= Xn;
    xyz.y *= Yn;
    xyz.z *= Zn;

    return xyz;
}

// XYZ to RGB conversion
half3 XYZtoRGB(half3 xyz) {
    half3 rgb;

    // Apply XYZ to RGB transformation matrix (D65)
    rgb.x =  3.2404542 * xyz.x - 1.5371385 * xyz.y - 0.4985314 * xyz.z;
    rgb.y = -0.9692660 * xyz.x + 1.8760108 * xyz.y + 0.0415560 * xyz.z;
    rgb.z =  0.0556434 * xyz.x - 0.2040259 * xyz.y + 1.0572252 * xyz.z;

    // Apply inverse gamma correction
    rgb = (rgb <= 0.0031308) ? rgb * 12.92 : pow(rgb, 1.0 / 2.4) * 1.055 - 0.055;

    return clamp(rgb, 0.0, 1.0);
}

// Lab to RGB conversion
half3 LabToRGB(half3 lab) {
    return XYZtoRGB(LabtoXYZ(lab));
}


// Delta-E 2000 color difference metric
half DeltaE2000(half3 lab1, half3 lab2) {
    // Calculate the C* values (chroma)
    half C1 = sqrt(lab1.y * lab1.y + lab1.z * lab1.z);
    half C2 = sqrt(lab2.y * lab2.y + lab2.z * lab2.z);

    // Calculate the mean C*
    half meanC = (C1 + C2) / 2.0;

    // Calculate the G factor (to account for hue shift)
    half G = 0.5 * (1.0 - sqrt(meanC * meanC / (meanC * meanC + 25.0)));

    // Adjust for hue shift
    half h1 = atan2(lab1.z, lab1.y);
    half h2 = atan2(lab2.z, lab2.y);
    half H1 = (h1 < 0.0) ? h1 + 6.283185 : h1; // Ensure hue is non-negative
    half H2 = (h2 < 0.0) ? h2 + 6.283185 : h2;

    half dH = H2 - H1;
    if (abs(dH) > 3.14159) dH -= 6.283185;

    half dL = lab2.x - lab1.x;
    half dC = C2 - C1;

    // Calculate the final deltaE value
    half term1 = dL / 1.0;
    half term2 = dC / (1.0 + 0.045 * meanC);
    half term3 = dH / (1.0 + 0.015 * meanC);

    half deltaE = sqrt(term1 * term1 + term2 * term2 + term3 * term3);

    return deltaE;
}




#endif