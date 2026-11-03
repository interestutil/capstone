package com.fathalla.wms.worker

import android.app.Application
import com.fathalla.wms.worker.data.SessionStore

class WmsApp : Application() {
    lateinit var sessionStore: SessionStore
        private set

    override fun onCreate() {
        super.onCreate()
        sessionStore = SessionStore(this)
    }
}
