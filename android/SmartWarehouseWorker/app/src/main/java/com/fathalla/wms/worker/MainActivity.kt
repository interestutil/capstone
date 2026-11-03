package com.fathalla.wms.worker

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import com.fathalla.wms.worker.ui.WorkerApp
import com.fathalla.wms.worker.ui.theme.WorkerTheme

class MainActivity : ComponentActivity() {
    private val viewModel: WmsViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            WorkerTheme {
                WorkerApp(viewModel)
            }
        }
    }
}
