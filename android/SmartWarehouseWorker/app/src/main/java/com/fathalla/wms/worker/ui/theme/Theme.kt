package com.fathalla.wms.worker.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val Ink = Color(0xFF1C241F)
private val Paper = Color(0xFFF3EEE4)
private val Amber = Color(0xFFE3A01A)
private val Panel = Color(0xFF24302B)

private val Colors = darkColorScheme(
    primary = Amber,
    onPrimary = Ink,
    background = Ink,
    onBackground = Paper,
    surface = Panel,
    onSurface = Paper,
    secondary = Amber
)

@Composable
fun WorkerTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = Colors, content = content)
}
